using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface ICartService
{
    Task<CartDto> GetAsync(string userId);
    Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(string userId, string bookId, int quantity);
    Task<CartDto> RemoveItemAsync(string userId, string bookId);
    Task<CartDto> ClearAsync(string userId);
    Task<CartDto> ApplyPromoAsync(string userId, string code);
    Task<CartDto> RemovePromoAsync(string userId);
}

public class CartService : ICartService
{
    private readonly MongoDbContext _db;
    private readonly IPromoService _promo;

    public CartService(MongoDbContext db, IPromoService promo)
    {
        _db = db;
        _promo = promo;
    }

    public async Task<CartDto> GetAsync(string userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return await MapAsync(cart);
    }

    public async Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request)
    {
        if (request.Quantity <= 0) throw AppException.BadRequest("Miqdar 1-dən böyük olmalıdır.");

        var book = await _db.Books.Find(b => b.Id == request.BookId).FirstOrDefaultAsync()
                   ?? throw AppException.NotFound("Kitab tapılmadı.");

        var cart = await GetOrCreateCartAsync(userId);
        var existing = cart.Items.FirstOrDefault(i => i.BookId == request.BookId);
        if (existing != null) existing.Quantity += request.Quantity;
        else cart.Items.Add(new CartItem { BookId = book.Id, Quantity = request.Quantity });

        return await SaveAndMapAsync(cart);
    }

    public async Task<CartDto> UpdateItemAsync(string userId, string bookId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => i.BookId == bookId)
                   ?? throw AppException.NotFound("Səbətdə tapılmadı.");

        if (quantity <= 0) cart.Items.Remove(item);
        else item.Quantity = quantity;

        return await SaveAndMapAsync(cart);
    }

    public async Task<CartDto> RemoveItemAsync(string userId, string bookId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        cart.Items.RemoveAll(i => i.BookId == bookId);
        return await SaveAndMapAsync(cart);
    }

    public async Task<CartDto> ClearAsync(string userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        cart.Items.Clear();
        cart.AppliedPromoCode = null;
        return await SaveAndMapAsync(cart);
    }

    public async Task<CartDto> ApplyPromoAsync(string userId, string code)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var subtotal = await CalcSubtotalAsync(cart);
        var validation = await _promo.ValidateAsync(code, subtotal);
        if (!validation.IsValid) throw AppException.BadRequest(validation.Message ?? "Promokod etibarsızdır.");

        cart.AppliedPromoCode = code.ToUpper();
        return await SaveAndMapAsync(cart);
    }

    public async Task<CartDto> RemovePromoAsync(string userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        cart.AppliedPromoCode = null;
        return await SaveAndMapAsync(cart);
    }

    private async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await _db.Carts.Find(c => c.UserId == userId).FirstOrDefaultAsync();
        if (cart != null) return cart;

        cart = new Cart { UserId = userId };
        await _db.Carts.InsertOneAsync(cart);
        return cart;
    }

    private async Task<decimal> CalcSubtotalAsync(Cart cart)
    {
        if (cart.Items.Count == 0) return 0;
        var bookIds = cart.Items.Select(i => i.BookId).ToList();
        var books = await _db.Books.Find(b => bookIds.Contains(b.Id)).ToListAsync();
        var bookMap = books.ToDictionary(b => b.Id);
        return cart.Items.Sum(i => bookMap.TryGetValue(i.BookId, out var b) ? b.Price * i.Quantity : 0);
    }

    private async Task<CartDto> SaveAndMapAsync(Cart cart)
    {
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.Carts.ReplaceOneAsync(c => c.Id == cart.Id, cart);
        return await MapAsync(cart);
    }

    private async Task<CartDto> MapAsync(Cart cart)
    {
        var bookIds = cart.Items.Select(i => i.BookId).ToList();
        var books = bookIds.Count > 0
            ? await _db.Books.Find(b => bookIds.Contains(b.Id)).ToListAsync()
            : new List<Book>();
        var bookMap = books.ToDictionary(b => b.Id);

        var items = cart.Items
            .Where(i => bookMap.ContainsKey(i.BookId))
            .Select(i =>
            {
                var b = bookMap[i.BookId];
                return new CartItemDto
                {
                    BookId = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    CoverImageUrl = b.CoverImageUrl,
                    Price = b.Price,
                    Quantity = i.Quantity,
                    LineTotal = b.Price * i.Quantity,
                    Stock = b.Stock
                };
            }).ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var discount = 0m;
        if (!string.IsNullOrEmpty(cart.AppliedPromoCode))
        {
            var validation = await _promo.ValidateAsync(cart.AppliedPromoCode, subtotal);
            if (validation.IsValid) discount = validation.DiscountAmount;
            else cart.AppliedPromoCode = null;
        }

        return new CartDto
        {
            Id = cart.Id,
            Items = items,
            Subtotal = subtotal,
            Discount = discount,
            Total = Math.Max(0, subtotal - discount),
            AppliedPromoCode = cart.AppliedPromoCode,
            ItemCount = items.Sum(i => i.Quantity)
        };
    }
}
