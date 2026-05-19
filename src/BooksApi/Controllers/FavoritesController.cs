using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favorites;
    private readonly ICurrentUserService _user;

    public FavoritesController(IFavoriteService favorites, ICurrentUserService user)
    {
        _favorites = favorites;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<FavoriteDto>>> List() =>
        Ok(await _favorites.ListAsync(_user.RequireUserId()));

    [HttpPost("{bookId}")]
    public async Task<IActionResult> Add(string bookId)
    {
        await _favorites.AddAsync(_user.RequireUserId(), bookId);
        return NoContent();
    }

    [HttpDelete("{bookId}")]
    public async Task<IActionResult> Remove(string bookId)
    {
        await _favorites.RemoveAsync(_user.RequireUserId(), bookId);
        return NoContent();
    }
}
