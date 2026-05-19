using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> ListAsync(string userId, OrderQuery query);
    Task<OrderDto> GetAsync(string userId, string id);
    Task<OrderDto> CreateFromCartAsync(string userId, CreateOrderRequest request);
    Task<OrderDto> CancelAsync(string userId, string id);
    Task<CartDto> RepeatAsync(string userId, string id, ICartService cart);
    Task<OrderTrackingDto> GetTrackingAsync(string userId, string id);
    Task<OrderDto> UpdateStatusAsync(string id, OrderStatus status);
}

public class OrderService : IOrderService
{
    private readonly MongoDbContext _db;
    private readonly IAddressService _addresses;
    private readonly IPromoService _promo;

    public OrderService(MongoDbContext db, IAddressService addresses, IPromoService promo)
    {
        _db = db;
        _addresses = addresses;
        _promo = promo;
    }

    public async Task<PagedResult<OrderDto>> ListAsync(string userId, OrderQuery query)
    {
        var filter = Builders<Order>.Filter.Eq(o => o.UserId, userId);

        if (!string.IsNullOrWhiteSpace(query.Status) && query.Status != "all")
        {
            var statuses = query.Status switch
            {
                "active" => new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.InTransit },
                "delivered" => new[] { OrderStatus.Delivered },
                "cancelled" => new[] { OrderStatus.Cancelled },
                _ => Array.Empty<OrderStatus>()
            };
            if (statuses.Length > 0)
                filter &= Builders<Order>.Filter.In(o => o.Status, statuses);
        }

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var total = await _db.Orders.CountDocumentsAsync(filter);
        var orders = await _db.Orders.Find(filter)
            .SortByDescending(o => o.CreatedAt)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync();

        return new PagedResult<OrderDto>
        {
            Items = orders.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<OrderDto> GetAsync(string userId, string id)
    {
        var order = await _db.Orders.Find(o => o.Id == id && o.UserId == userId).FirstOrDefaultAsync()
                    ?? throw AppException.NotFound("Sifariş tapılmadı.");
        return Map(order);
    }

    public async Task<OrderDto> CreateFromCartAsync(string userId, CreateOrderRequest request)
    {
        var cart = await _db.Carts.Find(c => c.UserId == userId).FirstOrDefaultAsync();
        if (cart == null || cart.Items.Count == 0)
            throw AppException.BadRequest("Səbət boşdur.");

        var address = await _addresses.RequireAsync(userId, request.AddressId);

        var bookIds = cart.Items.Select(i => i.BookId).ToList();
        var books = await _db.Books.Find(b => bookIds.Contains(b.Id)).ToListAsync();
        var bookMap = books.ToDictionary(b => b.Id);

        var items = new List<OrderItem>();
        decimal subtotal = 0;
        foreach (var ci in cart.Items)
        {
            if (!bookMap.TryGetValue(ci.BookId, out var b)) continue;
            if (b.Stock < ci.Quantity)
                throw AppException.BadRequest($"\"{b.Title}\" üçün kifayət qədər ehtiyat yoxdur.");
            items.Add(new OrderItem
            {
                BookId = b.Id,
                Title = b.Title,
                Author = b.Author,
                CoverImageUrl = b.CoverImageUrl,
                Price = b.Price,
                Quantity = ci.Quantity
            });
            subtotal += b.Price * ci.Quantity;
        }

        if (items.Count == 0) throw AppException.BadRequest("Səbətdə etibarlı məhsul yoxdur.");

        var deliveryMethod = Enum.TryParse<DeliveryMethod>(request.DeliveryMethod, true, out var dm) ? dm : DeliveryMethod.Standard;
        var deliveryFee = deliveryMethod == DeliveryMethod.Express ? 5.00m : 0m;

        decimal discount = 0;
        var promoCode = cart.AppliedPromoCode ?? request.PromoCode;
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var v = await _promo.ValidateAsync(promoCode, subtotal);
            if (!v.IsValid) throw AppException.BadRequest(v.Message ?? "Promokod etibarsızdır.");
            discount = v.DiscountAmount;
        }

        var paymentMethod = Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var pm) ? pm : PaymentMethod.Card;

        var orderNumber = $"KB-{Random.Shared.Next(10000, 99999)}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Items = items,
            DeliveryAddress = new AddressSnapshot
            {
                Label = address.Label,
                StreetLine = address.StreetLine,
                District = address.District,
                City = address.City,
                Country = address.Country,
                Phone = address.Phone
            },
            DeliveryMethod = deliveryMethod,
            PaymentMethod = paymentMethod,
            PaymentCardId = request.PaymentCardId,
            PromoCode = promoCode,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Discount = discount,
            Total = Math.Max(0, subtotal + deliveryFee - discount),
            Status = OrderStatus.Confirmed,
            EstimatedDeliveryAt = DateTime.UtcNow.AddDays(deliveryMethod == DeliveryMethod.Express ? 0 : 2)
        };

        await _db.Orders.InsertOneAsync(order);

        // Decrement stock
        foreach (var item in items)
        {
            await _db.Books.UpdateOneAsync(b => b.Id == item.BookId,
                Builders<Book>.Update.Inc(b => b.Stock, -item.Quantity));
        }

        // Clear cart
        await _db.Carts.UpdateOneAsync(c => c.Id == cart.Id,
            Builders<Cart>.Update
                .Set(c => c.Items, new List<CartItem>())
                .Set(c => c.AppliedPromoCode, null)
                .Set(c => c.UpdatedAt, DateTime.UtcNow));

        if (!string.IsNullOrEmpty(promoCode))
            await _promo.IncrementUsageAsync(promoCode);

        // Notify
        await _db.Notifications.InsertOneAsync(new Notification
        {
            UserId = userId,
            Type = NotificationType.OrderShipped,
            Title = "Sifarişiniz təsdiqləndi",
            Body = $"Sifariş #{order.OrderNumber} təsdiqləndi və hazırlanır.",
            Data = new Dictionary<string, string> { ["orderId"] = order.Id }
        });

        return Map(order);
    }

    public async Task<OrderDto> CancelAsync(string userId, string id)
    {
        var order = await _db.Orders.Find(o => o.Id == id && o.UserId == userId).FirstOrDefaultAsync()
                    ?? throw AppException.NotFound("Sifariş tapılmadı.");

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw AppException.BadRequest("Bu sifariş ləğv edilə bilməz.");

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.Orders.ReplaceOneAsync(o => o.Id == id, order);

        // Restock
        foreach (var item in order.Items)
        {
            await _db.Books.UpdateOneAsync(b => b.Id == item.BookId,
                Builders<Book>.Update.Inc(b => b.Stock, item.Quantity));
        }

        return Map(order);
    }

    public async Task<CartDto> RepeatAsync(string userId, string id, ICartService cart)
    {
        var order = await _db.Orders.Find(o => o.Id == id && o.UserId == userId).FirstOrDefaultAsync()
                    ?? throw AppException.NotFound("Sifariş tapılmadı.");

        await cart.ClearAsync(userId);
        foreach (var item in order.Items)
        {
            await cart.AddItemAsync(userId, new AddCartItemRequest { BookId = item.BookId, Quantity = item.Quantity });
        }
        return await cart.GetAsync(userId);
    }

    public async Task<OrderTrackingDto> GetTrackingAsync(string userId, string id)
    {
        var order = await _db.Orders.Find(o => o.Id == id && o.UserId == userId).FirstOrDefaultAsync()
                    ?? throw AppException.NotFound("Sifariş tapılmadı.");

        var currentStatus = (int)order.Status;
        var steps = new List<TrackingStep>
        {
            new() { Status = "Pending", Label = "Qəbul edildi", Completed = currentStatus >= 0, At = order.CreatedAt },
            new() { Status = "Confirmed", Label = "Təsdiqləndi", Completed = currentStatus >= 1 },
            new() { Status = "Preparing", Label = "Hazırlanır", Completed = currentStatus >= 2 },
            new() { Status = "InTransit", Label = "Yoldadır", Completed = currentStatus >= 3 },
            new() { Status = "Delivered", Label = "Çatdırıldı", Completed = currentStatus >= 4 }
        };

        return new OrderTrackingDto
        {
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Steps = steps,
            EstimatedDeliveryAt = order.EstimatedDeliveryAt
        };
    }

    public async Task<OrderDto> UpdateStatusAsync(string id, OrderStatus status)
    {
        var updated = await _db.Orders.FindOneAndUpdateAsync<Order>(o => o.Id == id,
            Builders<Order>.Update.Set(o => o.Status, status).Set(o => o.UpdatedAt, DateTime.UtcNow),
            new FindOneAndUpdateOptions<Order> { ReturnDocument = ReturnDocument.After })
            ?? throw AppException.NotFound("Sifariş tapılmadı.");
        return Map(updated);
    }

    private static OrderDto Map(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Items = o.Items.Select(i => new OrderItemDto
        {
            BookId = i.BookId,
            Title = i.Title,
            Author = i.Author,
            CoverImageUrl = i.CoverImageUrl,
            Price = i.Price,
            Quantity = i.Quantity
        }).ToList(),
        DeliveryAddress = new AddressDto
        {
            Label = o.DeliveryAddress.Label,
            StreetLine = o.DeliveryAddress.StreetLine,
            District = o.DeliveryAddress.District,
            City = o.DeliveryAddress.City,
            Country = o.DeliveryAddress.Country,
            Phone = o.DeliveryAddress.Phone
        },
        DeliveryMethod = o.DeliveryMethod.ToString(),
        PaymentMethod = o.PaymentMethod.ToString(),
        PromoCode = o.PromoCode,
        Subtotal = o.Subtotal,
        DeliveryFee = o.DeliveryFee,
        Discount = o.Discount,
        Total = o.Total,
        Status = o.Status.ToString(),
        EstimatedDeliveryAt = o.EstimatedDeliveryAt,
        CreatedAt = o.CreatedAt,
        ItemCount = o.Items.Sum(i => i.Quantity)
    };
}
