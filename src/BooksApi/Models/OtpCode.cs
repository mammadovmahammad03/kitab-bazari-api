using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BooksApi.Models;

public enum OtpPurpose
{
    Register = 0,
    PasswordReset = 1,
    PhoneVerify = 2,
    EmailVerify = 3
}

public class OtpCode
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Target { get; set; } = string.Empty; // email or phone
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
