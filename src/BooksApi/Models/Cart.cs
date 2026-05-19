using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BooksApi.Models;

public class Cart
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    public List<CartItem> Items { get; set; } = new();

    public string? AppliedPromoCode { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CartItem
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string BookId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
