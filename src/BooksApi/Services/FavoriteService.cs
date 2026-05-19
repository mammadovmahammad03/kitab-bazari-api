using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IFavoriteService
{
    Task<List<FavoriteDto>> ListAsync(string userId);
    Task AddAsync(string userId, string bookId);
    Task RemoveAsync(string userId, string bookId);
}

public class FavoriteService : IFavoriteService
{
    private readonly MongoDbContext _db;

    public FavoriteService(MongoDbContext db) => _db = db;

    public async Task<List<FavoriteDto>> ListAsync(string userId)
    {
        var favs = await _db.Favorites.Find(f => f.UserId == userId)
            .SortByDescending(f => f.CreatedAt).ToListAsync();

        if (favs.Count == 0) return new List<FavoriteDto>();

        var bookIds = favs.Select(f => f.BookId).ToList();
        var books = await _db.Books.Find(b => bookIds.Contains(b.Id)).ToListAsync();
        var bookMap = books.ToDictionary(b => b.Id);

        return favs.Where(f => bookMap.ContainsKey(f.BookId)).Select(f =>
        {
            var b = bookMap[f.BookId];
            return new FavoriteDto
            {
                BookId = b.Id,
                CreatedAt = f.CreatedAt,
                Book = new BookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    Price = b.Price,
                    Currency = b.Currency,
                    CoverImageUrl = b.CoverImageUrl,
                    Rating = b.Rating,
                    ReviewCount = b.ReviewCount,
                    IsFavorited = true
                }
            };
        }).ToList();
    }

    public async Task AddAsync(string userId, string bookId)
    {
        var book = await _db.Books.Find(b => b.Id == bookId).FirstOrDefaultAsync()
                   ?? throw AppException.NotFound("Kitab tapılmadı.");

        try
        {
            await _db.Favorites.InsertOneAsync(new Favorite { UserId = userId, BookId = book.Id });
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // already favorited — idempotent
        }
    }

    public async Task RemoveAsync(string userId, string bookId)
    {
        await _db.Favorites.DeleteOneAsync(f => f.UserId == userId && f.BookId == bookId);
    }
}
