using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IPromoService
{
    Task<PromoValidationResult> ValidateAsync(string code, decimal subtotal);
    Task<PromoCode> CreateAsync(PromoCode code);
    Task<List<PromoCode>> ListAsync();
    Task DeleteAsync(string id);
    Task IncrementUsageAsync(string code);
}

public class PromoService : IPromoService
{
    private readonly MongoDbContext _db;

    public PromoService(MongoDbContext db) => _db = db;

    public async Task<PromoValidationResult> ValidateAsync(string code, decimal subtotal)
    {
        var upper = code.Trim().ToUpper();
        var promo = await _db.PromoCodes.Find(p => p.Code == upper).FirstOrDefaultAsync();

        if (promo == null)
            return new PromoValidationResult { IsValid = false, Code = upper, Message = "Promokod tapılmadı." };

        if (!promo.IsActive)
            return new PromoValidationResult { IsValid = false, Code = upper, Message = "Promokod aktiv deyil." };

        if (DateTime.UtcNow < promo.ValidFrom || DateTime.UtcNow > promo.ValidTo)
            return new PromoValidationResult { IsValid = false, Code = upper, Message = "Promokodun vaxtı bitib." };

        if (subtotal < promo.MinOrderAmount)
            return new PromoValidationResult { IsValid = false, Code = upper, Message = $"Minimum sifariş məbləği: {promo.MinOrderAmount} AZN." };

        if (promo.MaxUsages.HasValue && promo.UsageCount >= promo.MaxUsages.Value)
            return new PromoValidationResult { IsValid = false, Code = upper, Message = "Promokodun istifadə limiti bitib." };

        var discount = promo.DiscountType == DiscountType.Percent
            ? Math.Round(subtotal * promo.Value / 100m, 2)
            : promo.Value;
        discount = Math.Min(discount, subtotal);

        return new PromoValidationResult { IsValid = true, Code = upper, DiscountAmount = discount };
    }

    public async Task<PromoCode> CreateAsync(PromoCode code)
    {
        code.Code = code.Code.Trim().ToUpper();
        var existing = await _db.PromoCodes.Find(p => p.Code == code.Code).FirstOrDefaultAsync();
        if (existing != null) throw AppException.Conflict("Bu promokod artıq mövcuddur.");
        await _db.PromoCodes.InsertOneAsync(code);
        return code;
    }

    public async Task<List<PromoCode>> ListAsync() =>
        await _db.PromoCodes.Find(_ => true).SortByDescending(p => p.ValidFrom).ToListAsync();

    public async Task DeleteAsync(string id)
    {
        var result = await _db.PromoCodes.DeleteOneAsync(p => p.Id == id);
        if (result.DeletedCount == 0) throw AppException.NotFound("Promokod tapılmadı.");
    }

    public async Task IncrementUsageAsync(string code)
    {
        await _db.PromoCodes.UpdateOneAsync(p => p.Code == code.Trim().ToUpper(),
            Builders<PromoCode>.Update.Inc(p => p.UsageCount, 1));
    }
}
