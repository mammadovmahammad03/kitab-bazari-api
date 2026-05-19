using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> ListAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<CategoryDto> UpdateAsync(string id, CreateCategoryRequest request);
    Task DeleteAsync(string id);
}

public class CategoryService : ICategoryService
{
    private readonly MongoDbContext _db;

    public CategoryService(MongoDbContext db) => _db = db;

    public async Task<List<CategoryDto>> ListAsync()
    {
        var cats = await _db.Categories.Find(_ => true).SortBy(c => c.SortOrder).ToListAsync();
        var counts = new Dictionary<string, int>();
        foreach (var c in cats)
        {
            counts[c.Id] = (int)await _db.Books.CountDocumentsAsync(b => b.CategoryId == c.Id);
        }
        return cats.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            IconUrl = c.IconUrl,
            BookCount = counts.GetValueOrDefault(c.Id)
        }).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var existing = await _db.Categories.Find(c => c.Slug == request.Slug).FirstOrDefaultAsync();
        if (existing != null) throw AppException.Conflict("Bu slug ilə kateqoriya artıq var.");

        var cat = new Category
        {
            Name = request.Name,
            Slug = request.Slug,
            IconUrl = request.IconUrl,
            SortOrder = request.SortOrder
        };
        await _db.Categories.InsertOneAsync(cat);
        return new CategoryDto { Id = cat.Id, Name = cat.Name, Slug = cat.Slug, IconUrl = cat.IconUrl };
    }

    public async Task<CategoryDto> UpdateAsync(string id, CreateCategoryRequest request)
    {
        var update = Builders<Category>.Update
            .Set(c => c.Name, request.Name)
            .Set(c => c.Slug, request.Slug)
            .Set(c => c.IconUrl, request.IconUrl)
            .Set(c => c.SortOrder, request.SortOrder);

        var updated = await _db.Categories.FindOneAndUpdateAsync<Category>(c => c.Id == id, update,
            new FindOneAndUpdateOptions<Category> { ReturnDocument = ReturnDocument.After })
            ?? throw AppException.NotFound("Kateqoriya tapılmadı.");

        return new CategoryDto { Id = updated.Id, Name = updated.Name, Slug = updated.Slug, IconUrl = updated.IconUrl };
    }

    public async Task DeleteAsync(string id)
    {
        var result = await _db.Categories.DeleteOneAsync(c => c.Id == id);
        if (result.DeletedCount == 0) throw AppException.NotFound("Kateqoriya tapılmadı.");
    }
}
