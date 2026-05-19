namespace BooksApi.Dtos;

public class BookDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AZN";
    public string? CoverImageUrl { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int Stock { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PageCount { get; set; }
    public string Language { get; set; } = "az";
    public int? PublishedYear { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsFavorited { get; set; }
}

public class BookQuery
{
    public string? Search { get; set; }
    public string? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? Featured { get; set; }
    public string Sort { get; set; } = "newest"; // newest | price_asc | price_desc | rating
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int BookCount { get; set; }
}

public class CreateBookRequest
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "AZN";
    public string? CoverImageUrl { get; set; }
    public string? CategoryId { get; set; }
    public int Stock { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public int? PageCount { get; set; }
    public string Language { get; set; } = "az";
    public int? PublishedYear { get; set; }
    public bool IsFeatured { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int SortOrder { get; set; }
}
