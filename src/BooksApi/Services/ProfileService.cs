using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IProfileService
{
    Task<ProfileDto> GetAsync(string userId);
    Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task DeleteAsync(string userId);
    Task<UserSettingsDto> GetSettingsAsync(string userId);
    Task<UserSettingsDto> UpdateSettingsAsync(string userId, UserSettingsDto settings);
    Task<ProfileStatsDto> GetStatsAsync(string userId);
}

public class ProfileService : IProfileService
{
    private readonly MongoDbContext _db;

    public ProfileService(MongoDbContext db) => _db = db;

    public async Task<ProfileDto> GetAsync(string userId)
    {
        var user = await RequireAsync(userId);
        var stats = await GetStatsAsync(userId);
        return new ProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            EmailVerified = user.EmailVerified,
            PhoneVerified = user.PhoneVerified,
            CreatedAt = user.CreatedAt,
            Stats = stats
        };
    }

    public async Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request)
    {
        var update = Builders<User>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow);
        if (request.FullName != null) update = update.Set(u => u.FullName, request.FullName);
        if (request.Phone != null) update = update.Set(u => u.Phone, request.Phone);
        if (request.AvatarUrl != null) update = update.Set(u => u.AvatarUrl, request.AvatarUrl);

        await _db.Users.UpdateOneAsync(u => u.Id == userId, update);
        return await GetAsync(userId);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await RequireAsync(userId);
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw AppException.BadRequest("Cari şifrə yanlışdır.");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.Users.UpdateOneAsync(u => u.Id == userId,
            Builders<User>.Update.Set(u => u.PasswordHash, hash).Set(u => u.UpdatedAt, DateTime.UtcNow));
    }

    public async Task DeleteAsync(string userId)
    {
        await _db.Users.DeleteOneAsync(u => u.Id == userId);
        await _db.Carts.DeleteManyAsync(c => c.UserId == userId);
        await _db.Favorites.DeleteManyAsync(f => f.UserId == userId);
        await _db.Addresses.DeleteManyAsync(a => a.UserId == userId);
        await _db.PaymentCards.DeleteManyAsync(c => c.UserId == userId);
        await _db.Notifications.DeleteManyAsync(n => n.UserId == userId);
        await _db.RefreshTokens.DeleteManyAsync(r => r.UserId == userId);
        // Orders and reviews are kept for audit trail.
    }

    public async Task<UserSettingsDto> GetSettingsAsync(string userId)
    {
        var user = await RequireAsync(userId);
        return new UserSettingsDto
        {
            NotificationsEnabled = user.Settings.NotificationsEnabled,
            Language = user.Settings.Language,
            DarkMode = user.Settings.DarkMode
        };
    }

    public async Task<UserSettingsDto> UpdateSettingsAsync(string userId, UserSettingsDto settings)
    {
        var update = Builders<User>.Update
            .Set(u => u.Settings.NotificationsEnabled, settings.NotificationsEnabled)
            .Set(u => u.Settings.Language, settings.Language)
            .Set(u => u.Settings.DarkMode, settings.DarkMode)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);
        await _db.Users.UpdateOneAsync(u => u.Id == userId, update);
        return settings;
    }

    public async Task<ProfileStatsDto> GetStatsAsync(string userId)
    {
        var deliveredOrders = await _db.Orders.Find(o => o.UserId == userId && o.Status == OrderStatus.Delivered).ToListAsync();
        return new ProfileStatsDto
        {
            BooksPurchased = deliveredOrders.Sum(o => o.Items.Sum(i => i.Quantity)),
            FavoritesCount = (int)await _db.Favorites.CountDocumentsAsync(f => f.UserId == userId),
            OrdersCount = (int)await _db.Orders.CountDocumentsAsync(o => o.UserId == userId)
        };
    }

    private async Task<User> RequireAsync(string userId) =>
        await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
        ?? throw AppException.NotFound("İstifadəçi tapılmadı.");
}
