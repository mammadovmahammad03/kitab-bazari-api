using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IPaymentCardService
{
    Task<List<PaymentCardDto>> ListAsync(string userId);
    Task<PaymentCardDto> CreateAsync(string userId, CreatePaymentCardRequest request);
    Task DeleteAsync(string userId, string id);
    Task<PaymentCardDto> SetDefaultAsync(string userId, string id);
}

public class PaymentCardService : IPaymentCardService
{
    private readonly MongoDbContext _db;

    public PaymentCardService(MongoDbContext db) => _db = db;

    public async Task<List<PaymentCardDto>> ListAsync(string userId)
    {
        var cards = await _db.PaymentCards.Find(c => c.UserId == userId)
            .SortByDescending(c => c.IsDefault).ThenByDescending(c => c.CreatedAt).ToListAsync();
        return cards.Select(Map).ToList();
    }

    public async Task<PaymentCardDto> CreateAsync(string userId, CreatePaymentCardRequest request)
    {
        var digits = new string(request.CardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 13) throw AppException.BadRequest("Kart nömrəsi yanlışdır.");

        var brand = DetectBrand(digits);
        var lastFour = digits[^4..];

        var card = new PaymentCard
        {
            UserId = userId,
            Brand = brand,
            LastFour = lastFour,
            HolderName = request.HolderName.ToUpper(),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Token = Guid.NewGuid().ToString("N"), // stub — real PSP would return this
            IsDefault = request.SetAsDefault
        };

        await _db.PaymentCards.InsertOneAsync(card);
        if (request.SetAsDefault) await UnsetOtherDefaultsAsync(userId, card.Id);

        return Map(card);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var result = await _db.PaymentCards.DeleteOneAsync(c => c.Id == id && c.UserId == userId);
        if (result.DeletedCount == 0) throw AppException.NotFound("Kart tapılmadı.");
    }

    public async Task<PaymentCardDto> SetDefaultAsync(string userId, string id)
    {
        var card = await _db.PaymentCards.Find(c => c.Id == id && c.UserId == userId).FirstOrDefaultAsync()
                   ?? throw AppException.NotFound("Kart tapılmadı.");

        await UnsetOtherDefaultsAsync(userId, id);
        await _db.PaymentCards.UpdateOneAsync(c => c.Id == id,
            Builders<PaymentCard>.Update.Set(c => c.IsDefault, true));
        card.IsDefault = true;
        return Map(card);
    }

    private async Task UnsetOtherDefaultsAsync(string userId, string keepId)
    {
        await _db.PaymentCards.UpdateManyAsync(
            c => c.UserId == userId && c.Id != keepId,
            Builders<PaymentCard>.Update.Set(c => c.IsDefault, false));
    }

    private static string DetectBrand(string digits)
    {
        if (digits.StartsWith("4")) return "Visa";
        if (digits.StartsWith("5") || digits.StartsWith("2")) return "Mastercard";
        if (digits.StartsWith("34") || digits.StartsWith("37")) return "Amex";
        return "Other";
    }

    private static PaymentCardDto Map(PaymentCard c) => new()
    {
        Id = c.Id,
        Brand = c.Brand,
        LastFour = c.LastFour,
        HolderName = c.HolderName,
        ExpiryMonth = c.ExpiryMonth,
        ExpiryYear = c.ExpiryYear,
        IsDefault = c.IsDefault
    };
}
