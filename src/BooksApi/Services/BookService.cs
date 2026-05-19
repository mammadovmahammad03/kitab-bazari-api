using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IBookService
{
    Task<PagedResult<BookDto>> ListAsync(BookQuery query, string? userId);
    Task<BookDto> GetAsync(string id, string? userId);
    Task<List<BookDto>> GetFeaturedAsync(string? userId, int limit = 10);
    Task<BookDto> CreateAsync(CreateBookRequest request);
    Task<BookDto> UpdateAsync(string id, CreateBookRequest request);
    Task DeleteAsync(string id);
}

public class BookService : IBookService
{
    private readonly MongoDbContext _db;

    public BookService(MongoDbContext db) => _db = db;

    public async Task<PagedResult<BookDto>> ListAsync(BookQuery query, string? userId)
    {
        var filter = Builders<Book>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var regex = new MongoDB.Bson.BsonRegularExpression(query.Search, "i");
            filter &= Builders<Book>.Filter.Or(
                Builders<Book>.Filter.Regex(b => b.Title, regex),
                Builders<Book>.Filter.Regex(b => b.Author, regex));
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId))
            filter &= Builders<Book>.Filter.Eq(b => b.CategoryId, query.CategoryId);

        if (query.MinPrice.HasValue)
            filter &= Builders<Book>.Filter.Gte(b => b.Price, query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            filter &= Builders<Book>.Filter.Lte(b => b.Price, query.MaxPrice.Value);

        if (query.Featured == true)
            filter &= Builders<Book>.Filter.Eq(b => b.IsFeatured, true);

        var sort = query.Sort switch
        {
            "price_asc" => Builders<Book>.Sort.Ascending(b => b.Price),
            "price_desc" => Builders<Book>.Sort.Descending(b => b.Price),
            "rating" => Builders<Book>.Sort.Descending(b => b.Rating),
            _ => Builders<Book>.Sort.Descending(b => b.CreatedAt)
        };

        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var total = await _db.Books.CountDocumentsAsync(filter);
        var items = await _db.Books.Find(filter)
            .Sort(sort)
            .Skip((page - 1) * size)
            .Limit(size)
            .ToListAsync();

        var dtos = await MapWithExtrasAsync(items, userId);

        return new PagedResult<BookDto> { Items = dtos, TotalCount = total, Page = page, PageSize = size };
    }

    public async Task<BookDto> GetAsync(string id, string? userId)
    {
        var book = await _db.Books.Find(b => b.Id == id).FirstOrDefaultAsync()
                   ?? throw AppException.NotFound("Kitab tapılmadı.");
        var mapped = await MapWithExtrasAsync(new List<Book> { book }, userId);
        return mapped[0];
    }

    public async Task<List<BookDto>> GetFeaturedAsync(string? userId, int limit = 10)
    {
        var items = await _db.Books.Find(b => b.IsFeatured)
            .SortByDescending(b => b.CreatedAt)
            .Limit(limit)
            .ToListAsync();
        return await MapWithExtrasAsync(items, userId);
    }

    public async Task<BookDto> CreateAsync(CreateBookRequest request)
    {
        var book = new Book
        {
            Title = request.Title,
            Author = request.Author,
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency,
            CoverImageUrl = request.CoverImageUrl,
            CategoryId = request.CategoryId,
            Stock = request.Stock,
            Isbn = request.Isbn,
            Publisher = request.Publisher,
            PageCount = request.PageCount,
            Language = request.Language,
            PublishedYear = request.PublishedYear,
            IsFeatured = request.IsFeatured
        };
        await _db.Books.InsertOneAsync(book);
        return (await MapWithExtrasAsync(new List<Book> { book }, null))[0];
    }

    public async Task<BookDto> UpdateAsync(string id, CreateBookRequest request)
    {
        var update = Builders<Book>.Update
            .Set(b => b.Title, request.Title)
            .Set(b => b.Author, request.Author)
            .Set(b => b.Description, request.Description)
            .Set(b => b.Price, request.Price)
            .Set(b => b.Currency, request.Currency)
            .Set(b => b.CoverImageUrl, request.CoverImageUrl)
            .Set(b => b.CategoryId, request.CategoryId)
            .Set(b => b.Stock, request.Stock)
            .Set(b => b.Isbn, request.Isbn)
            .Set(b => b.Publisher, request.Publisher)
            .Set(b => b.PageCount, request.PageCount)
            .Set(b => b.Language, request.Language)
            .Set(b => b.PublishedYear, request.PublishedYear)
            .Set(b => b.IsFeatured, request.IsFeatured)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var updated = await _db.Books.FindOneAndUpdateAsync<Book>(b => b.Id == id, update,
            new FindOneAndUpdateOptions<Book> { ReturnDocument = ReturnDocument.After })
            ?? throw AppException.NotFound("Kitab tapılmadı.");

        return (await MapWithExtrasAsync(new List<Book> { updated }, null))[0];
    }

    public async Task DeleteAsync(string id)
    {
        var result = await _db.Books.DeleteOneAsync(b => b.Id == id);
        if (result.DeletedCount == 0) throw AppException.NotFound("Kitab tapılmadı.");
    }

    private async Task<List<BookDto>> MapWithExtrasAsync(List<Book> books, string? userId)
    {
        if (books.Count == 0) return new List<BookDto>();

        var categoryIds = books.Where(b => b.CategoryId != null).Select(b => b.CategoryId!).Distinct().ToList();
        var categories = categoryIds.Count > 0
            ? await _db.Categories.Find(c => categoryIds.Contains(c.Id)).ToListAsync()
            : new List<Category>();
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        HashSet<string> favoritedBookIds = new();
        if (!string.IsNullOrEmpty(userId))
        {
            var bookIds = books.Select(b => b.Id).ToList();
            var favs = await _db.Favorites.Find(f => f.UserId == userId && bookIds.Contains(f.BookId)).ToListAsync();
            favoritedBookIds = favs.Select(f => f.BookId).ToHashSet();
        }

        return books.Select(b => new BookDto
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            Description = b.Description,
            Price = b.Price,
            Currency = b.Currency,
            CoverImageUrl = b.CoverImageUrl,
            CategoryId = b.CategoryId,
            CategoryName = b.CategoryId != null && categoryMap.TryGetValue(b.CategoryId, out var n) ? n : null,
            Stock = b.Stock,
            Isbn = b.Isbn,
            Publisher = b.Publisher,
            PageCount = b.PageCount,
            Language = b.Language,
            PublishedYear = b.PublishedYear,
            Rating = b.Rating,
            ReviewCount = b.ReviewCount,
            IsFavorited = favoritedBookIds.Contains(b.Id)
        }).ToList();
    }
}
