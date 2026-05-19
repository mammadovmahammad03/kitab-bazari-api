using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> ListAsync(string userId);
    Task MarkReadAsync(string userId, string id);
    Task MarkAllReadAsync(string userId);
    Task DeleteAsync(string userId, string id);
    Task<int> GetUnreadCountAsync(string userId);
    Task CreateAsync(string userId, NotificationType type, string title, string body, Dictionary<string, string>? data = null);
}

public class NotificationService : INotificationService
{
    private readonly MongoDbContext _db;

    public NotificationService(MongoDbContext db) => _db = db;

    public async Task<List<NotificationDto>> ListAsync(string userId)
    {
        var items = await _db.Notifications.Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAt).Limit(100).ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task MarkReadAsync(string userId, string id)
    {
        await _db.Notifications.UpdateOneAsync(n => n.Id == id && n.UserId == userId,
            Builders<Notification>.Update.Set(n => n.IsRead, true));
    }

    public async Task MarkAllReadAsync(string userId)
    {
        await _db.Notifications.UpdateManyAsync(n => n.UserId == userId && !n.IsRead,
            Builders<Notification>.Update.Set(n => n.IsRead, true));
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var result = await _db.Notifications.DeleteOneAsync(n => n.Id == id && n.UserId == userId);
        if (result.DeletedCount == 0) throw AppException.NotFound("Bildiriş tapılmadı.");
    }

    public async Task<int> GetUnreadCountAsync(string userId) =>
        (int)await _db.Notifications.CountDocumentsAsync(n => n.UserId == userId && !n.IsRead);

    public async Task CreateAsync(string userId, NotificationType type, string title, string body, Dictionary<string, string>? data = null)
    {
        await _db.Notifications.InsertOneAsync(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Data = data
        });
    }

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type.ToString(),
        Title = n.Title,
        Body = n.Body,
        Data = n.Data,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
