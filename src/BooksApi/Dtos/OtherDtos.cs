using System.ComponentModel.DataAnnotations;

namespace BooksApi.Dtos;

public class ReviewDto
{
    public string Id { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? UserDisplayName { get; set; }
    public string? UserAvatarUrl { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    [Required] public string BookId { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    [Required, Range(1, 5)] public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FavoriteDto
{
    public string BookId { get; set; } = string.Empty;
    public BookDto Book { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
