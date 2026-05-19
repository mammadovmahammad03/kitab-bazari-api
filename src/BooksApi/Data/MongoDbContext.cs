using BooksApi.Configuration;
using BooksApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BooksApi.Data;

public class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<User> Users => Database.GetCollection<User>("users");
    public IMongoCollection<Book> Books => Database.GetCollection<Book>("books");
    public IMongoCollection<Category> Categories => Database.GetCollection<Category>("categories");
    public IMongoCollection<Cart> Carts => Database.GetCollection<Cart>("carts");
    public IMongoCollection<Favorite> Favorites => Database.GetCollection<Favorite>("favorites");
    public IMongoCollection<Address> Addresses => Database.GetCollection<Address>("addresses");
    public IMongoCollection<PaymentCard> PaymentCards => Database.GetCollection<PaymentCard>("payment_cards");
    public IMongoCollection<Order> Orders => Database.GetCollection<Order>("orders");
    public IMongoCollection<PromoCode> PromoCodes => Database.GetCollection<PromoCode>("promo_codes");
    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>("reviews");
    public IMongoCollection<Notification> Notifications => Database.GetCollection<Notification>("notifications");
    public IMongoCollection<OtpCode> OtpCodes => Database.GetCollection<OtpCode>("otp_codes");
    public IMongoCollection<RefreshToken> RefreshTokens => Database.GetCollection<RefreshToken>("refresh_tokens");

    public async Task EnsureIndexesAsync()
    {
        await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));

        await Books.Indexes.CreateOneAsync(new CreateIndexModel<Book>(
            Builders<Book>.IndexKeys.Text(b => b.Title).Text(b => b.Author).Text(b => b.Description)));

        await Books.Indexes.CreateOneAsync(new CreateIndexModel<Book>(
            Builders<Book>.IndexKeys.Ascending(b => b.CategoryId)));

        await Categories.Indexes.CreateOneAsync(new CreateIndexModel<Category>(
            Builders<Category>.IndexKeys.Ascending(c => c.Slug),
            new CreateIndexOptions { Unique = true }));

        await Carts.Indexes.CreateOneAsync(new CreateIndexModel<Cart>(
            Builders<Cart>.IndexKeys.Ascending(c => c.UserId),
            new CreateIndexOptions { Unique = true }));

        await Favorites.Indexes.CreateOneAsync(new CreateIndexModel<Favorite>(
            Builders<Favorite>.IndexKeys.Ascending(f => f.UserId).Ascending(f => f.BookId),
            new CreateIndexOptions { Unique = true }));

        await Addresses.Indexes.CreateOneAsync(new CreateIndexModel<Address>(
            Builders<Address>.IndexKeys.Ascending(a => a.UserId)));

        await Orders.Indexes.CreateOneAsync(new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(o => o.UserId).Descending(o => o.CreatedAt)));

        await Orders.Indexes.CreateOneAsync(new CreateIndexModel<Order>(
            Builders<Order>.IndexKeys.Ascending(o => o.OrderNumber),
            new CreateIndexOptions { Unique = true }));

        await PromoCodes.Indexes.CreateOneAsync(new CreateIndexModel<PromoCode>(
            Builders<PromoCode>.IndexKeys.Ascending(p => p.Code),
            new CreateIndexOptions { Unique = true }));

        await Notifications.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(n => n.UserId).Descending(n => n.CreatedAt)));

        await OtpCodes.Indexes.CreateOneAsync(new CreateIndexModel<OtpCode>(
            Builders<OtpCode>.IndexKeys.Ascending(o => o.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));

        await RefreshTokens.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(r => r.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }));
    }
}
