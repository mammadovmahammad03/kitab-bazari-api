using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BooksApi.Models;

public class Review
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string BookId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? OrderId { get; set; }

    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }

    public string? UserDisplayName { get; set; }
    public string? UserAvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
