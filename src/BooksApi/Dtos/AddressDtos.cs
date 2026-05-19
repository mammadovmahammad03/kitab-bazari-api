using System.ComponentModel.DataAnnotations;

namespace BooksApi.Dtos;

public class AddressDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string StreetLine { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateAddressRequest
{
    [Required] public string Label { get; set; } = "Ev";
    [Required] public string StreetLine { get; set; } = string.Empty;
    [Required] public string District { get; set; } = string.Empty;
    [Required] public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Azərbaycan";
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateAddressRequest : CreateAddressRequest { }
