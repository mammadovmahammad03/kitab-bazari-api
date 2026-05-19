namespace BooksApi.Dtos;

public class CartDto
{
    public string Id { get; set; } = string.Empty;
    public List<CartItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? AppliedPromoCode { get; set; }
    public int ItemCount { get; set; }
}

public class CartItemDto
{
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int Stock { get; set; }
}

public class AddCartItemRequest
{
    public string BookId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    public int Quantity { get; set; }
}

public class ApplyPromoRequest
{
    public string Code { get; set; } = string.Empty;
}

public class PromoValidationResult
{
    public bool IsValid { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Message { get; set; }
    public decimal DiscountAmount { get; set; }
}
