using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BooksApi.Models;

public enum DiscountType
{
    Fixed = 0,
    Percent = 1
}

public class PromoCode
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.Fixed;
    public decimal Value { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddYears(1);
    public bool IsActive { get; set; } = true;
    public int? MaxUsages { get; set; }
    public int UsageCount { get; set; }
}
