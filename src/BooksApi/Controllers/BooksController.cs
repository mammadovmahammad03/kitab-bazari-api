using BooksApi.Common;
using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _books;
    private readonly ICurrentUserService _user;

    public BooksController(IBookService books, ICurrentUserService user)
    {
        _books = books;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookDto>>> List([FromQuery] BookQuery query) =>
        Ok(await _books.ListAsync(query, _user.UserId));

    [HttpGet("featured")]
    public async Task<ActionResult<List<BookDto>>> Featured([FromQuery] int limit = 10) =>
        Ok(await _books.GetFeaturedAsync(_user.UserId, limit));

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<BookDto>>> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _books.ListAsync(new BookQuery { Search = q, Page = page, PageSize = pageSize }, _user.UserId));

    [HttpGet("by-category/{categoryId}")]
    public async Task<ActionResult<PagedResult<BookDto>>> ByCategory(string categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _books.ListAsync(new BookQuery { CategoryId = categoryId, Page = page, PageSize = pageSize }, _user.UserId));

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> Get(string id) =>
        Ok(await _books.GetAsync(id, _user.UserId));

    // Admin endpoints
    [HttpPost, Authorize(Roles = "admin")]
    public async Task<ActionResult<BookDto>> Create([FromBody] CreateBookRequest request) =>
        Ok(await _books.CreateAsync(request));

    [HttpPut("{id}"), Authorize(Roles = "admin")]
    public async Task<ActionResult<BookDto>> Update(string id, [FromBody] CreateBookRequest request) =>
        Ok(await _books.UpdateAsync(id, request));

    [HttpDelete("{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await _books.DeleteAsync(id);
        return NoContent();
    }
}
