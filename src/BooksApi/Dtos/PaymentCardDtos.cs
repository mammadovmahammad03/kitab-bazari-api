using System.ComponentModel.DataAnnotations;

namespace BooksApi.Dtos;

public class PaymentCardDto
{
    public string Id { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
}

public class CreatePaymentCardRequest
{
    [Required, MinLength(13), MaxLength(19)] public string CardNumber { get; set; } = string.Empty;
    [Required] public string HolderName { get; set; } = string.Empty;
    [Required, Range(1, 12)] public int ExpiryMonth { get; set; }
    [Required, Range(2024, 2050)] public int ExpiryYear { get; set; }
    [Required, MinLength(3), MaxLength(4)] public string Cvv { get; set; } = string.Empty;
    public bool SetAsDefault { get; set; }
}
