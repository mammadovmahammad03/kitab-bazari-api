using BooksApi.Common;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IAddressService
{
    Task<List<AddressDto>> ListAsync(string userId);
    Task<AddressDto> CreateAsync(string userId, CreateAddressRequest request);
    Task<AddressDto> UpdateAsync(string userId, string id, UpdateAddressRequest request);
    Task DeleteAsync(string userId, string id);
    Task<AddressDto> SetDefaultAsync(string userId, string id);
    Task<Address> RequireAsync(string userId, string id);
}

public class AddressService : IAddressService
{
    private readonly MongoDbContext _db;

    public AddressService(MongoDbContext db) => _db = db;

    public async Task<List<AddressDto>> ListAsync(string userId)
    {
        var addrs = await _db.Addresses.Find(a => a.UserId == userId)
            .SortByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedAt).ToListAsync();
        return addrs.Select(Map).ToList();
    }

    public async Task<AddressDto> CreateAsync(string userId, CreateAddressRequest request)
    {
        var addr = new Address
        {
            UserId = userId,
            Label = request.Label,
            StreetLine = request.StreetLine,
            District = request.District,
            City = request.City,
            Country = request.Country,
            Phone = request.Phone,
            IsDefault = request.IsDefault
        };
        await _db.Addresses.InsertOneAsync(addr);
        if (request.IsDefault) await UnsetOtherDefaultsAsync(userId, addr.Id);
        return Map(addr);
    }

    public async Task<AddressDto> UpdateAsync(string userId, string id, UpdateAddressRequest request)
    {
        var update = Builders<Address>.Update
            .Set(a => a.Label, request.Label)
            .Set(a => a.StreetLine, request.StreetLine)
            .Set(a => a.District, request.District)
            .Set(a => a.City, request.City)
            .Set(a => a.Country, request.Country)
            .Set(a => a.Phone, request.Phone)
            .Set(a => a.IsDefault, request.IsDefault)
            .Set(a => a.UpdatedAt, DateTime.UtcNow);

        var updated = await _db.Addresses.FindOneAndUpdateAsync<Address>(
            a => a.Id == id && a.UserId == userId, update,
            new FindOneAndUpdateOptions<Address> { ReturnDocument = ReturnDocument.After })
            ?? throw AppException.NotFound("Ünvan tapılmadı.");

        if (request.IsDefault) await UnsetOtherDefaultsAsync(userId, id);
        return Map(updated);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var result = await _db.Addresses.DeleteOneAsync(a => a.Id == id && a.UserId == userId);
        if (result.DeletedCount == 0) throw AppException.NotFound("Ünvan tapılmadı.");
    }

    public async Task<AddressDto> SetDefaultAsync(string userId, string id)
    {
        var addr = await RequireAsync(userId, id);
        await UnsetOtherDefaultsAsync(userId, id);
        await _db.Addresses.UpdateOneAsync(a => a.Id == id,
            Builders<Address>.Update.Set(a => a.IsDefault, true));
        addr.IsDefault = true;
        return Map(addr);
    }

    public async Task<Address> RequireAsync(string userId, string id)
    {
        return await _db.Addresses.Find(a => a.Id == id && a.UserId == userId).FirstOrDefaultAsync()
               ?? throw AppException.NotFound("Ünvan tapılmadı.");
    }

    private async Task UnsetOtherDefaultsAsync(string userId, string keepId)
    {
        await _db.Addresses.UpdateManyAsync(
            a => a.UserId == userId && a.Id != keepId,
            Builders<Address>.Update.Set(a => a.IsDefault, false));
    }

    private static AddressDto Map(Address a) => new()
    {
        Id = a.Id,
        Label = a.Label,
        StreetLine = a.StreetLine,
        District = a.District,
        City = a.City,
        Country = a.Country,
        Phone = a.Phone,
        IsDefault = a.IsDefault
    };
}
