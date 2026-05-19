using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IReviewService
{
    Task<List<ReviewDto>> ListByBookAsync(string bookId);
    Task<ReviewDto> CreateAsync(string userId, CreateReviewRequest request);
}

public class ReviewService : IReviewService
{
    private readonly MongoDbContext _db;

    public ReviewService(MongoDbContext db) => _db = db;

    public async Task<List<ReviewDto>> ListByBookAsync(string bookId)
    {
        var reviews = await _db.Reviews.Find(r => r.BookId == bookId)
            .SortByDescending(r => r.CreatedAt).Limit(200).ToListAsync();
        return reviews.Select(Map).ToList();
    }

    public async Task<ReviewDto> CreateAsync(string userId, CreateReviewRequest request)
    {
        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
                   ?? throw AppException.Unauthorized();

        var book = await _db.Books.Find(b => b.Id == request.BookId).FirstOrDefaultAsync()
                   ?? throw AppException.NotFound("Kitab tapılmadı.");

        if (!string.IsNullOrEmpty(request.OrderId))
        {
            var owns = await _db.Orders.Find(o => o.Id == request.OrderId && o.UserId == userId).AnyAsync();
            if (!owns) throw AppException.Forbidden("Bu sifariş sizə aid deyil.");
        }

        var review = new Review
        {
            UserId = userId,
            BookId = book.Id,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Comment = request.Comment,
            UserDisplayName = user.FullName,
            UserAvatarUrl = user.AvatarUrl
        };
        await _db.Reviews.InsertOneAsync(review);

        // Update aggregated rating on book
        var all = await _db.Reviews.Find(r => r.BookId == book.Id).ToListAsync();
        var newAvg = all.Count > 0 ? Math.Round(all.Average(r => r.Rating), 2) : 0;
        await _db.Books.UpdateOneAsync(b => b.Id == book.Id,
            Builders<Book>.Update
                .Set(b => b.Rating, newAvg)
                .Set(b => b.ReviewCount, all.Count)
                .Set(b => b.UpdatedAt, DateTime.UtcNow));

        return Map(review);
    }

    private static ReviewDto Map(Review r) => new()
    {
        Id = r.Id,
        BookId = r.BookId,
        UserId = r.UserId,
        UserDisplayName = r.UserDisplayName,
        UserAvatarUrl = r.UserAvatarUrl,
        Rating = r.Rating,
        Comment = r.Comment,
        CreatedAt = r.CreatedAt
    };
}
