using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BooksApi.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    InTransit = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum DeliveryMethod
{
    Standard = 0,
    Express = 1
}

public enum PaymentMethod
{
    Card = 0,
    CashOnDelivery = 1,
    MilliOn = 2
}

public class Order
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string OrderNumber { get; set; } = string.Empty; // KB-90421

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    public List<OrderItem> Items { get; set; } = new();

    public AddressSnapshot DeliveryAddress { get; set; } = new();
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Standard;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? PaymentCardId { get; set; }

    public string? PromoCode { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class AddressSnapshot
{
    public string Label { get; set; } = string.Empty;
    public string StreetLine { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
