using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BooksApi.Models;

public class Book
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AZN";
    public string? CoverImageUrl { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? CategoryId { get; set; }

    public int Stock { get; set; } = 0;
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PageCount { get; set; }
    public string Language { get; set; } = "az";
    public int? PublishedYear { get; set; }

    public double Rating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;
    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
