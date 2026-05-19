using BooksApi.Models;

namespace BooksApi.Dtos;

public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
    public AddressDto DeliveryAddress { get; set; } = new();
    public string DeliveryMethod { get; set; } = "Standard";
    public string PaymentMethod { get; set; } = "Card";
    public string? PromoCode { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class OrderItemDto
{
    public string BookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    public string AddressId { get; set; } = string.Empty;
    public string DeliveryMethod { get; set; } = "Standard"; // Standard | Express
    public string PaymentMethod { get; set; } = "Card"; // Card | CashOnDelivery | MilliOn
    public string? PaymentCardId { get; set; }
    public string? PromoCode { get; set; }
}

public class OrderQuery
{
    public string? Status { get; set; } // all | active | delivered | cancelled
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class OrderTrackingDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<TrackingStep> Steps { get; set; } = new();
    public DateTime? EstimatedDeliveryAt { get; set; }
}

public class TrackingStep
{
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public DateTime? At { get; set; }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
