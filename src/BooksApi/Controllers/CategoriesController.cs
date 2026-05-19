using BooksApi.Common;
using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;
    private readonly IBookService _books;
    private readonly ICurrentUserService _user;

    public CategoriesController(ICategoryService categories, IBookService books, ICurrentUserService user)
    {
        _categories = categories;
        _books = books;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> List() => Ok(await _categories.ListAsync());

    [HttpGet("{id}/books")]
    public async Task<ActionResult<PagedResult<BookDto>>> Books(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _books.ListAsync(new BookQuery { CategoryId = id, Page = page, PageSize = pageSize }, _user.UserId));

    [HttpPost, Authorize(Roles = "admin")]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request) =>
        Ok(await _categories.CreateAsync(request));

    [HttpPut("{id}"), Authorize(Roles = "admin")]
    public async Task<ActionResult<CategoryDto>> Update(string id, [FromBody] CreateCategoryRequest request) =>
        Ok(await _categories.UpdateAsync(id, request));

    [HttpDelete("{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await _categories.DeleteAsync(id);
        return NoContent();
    }
}
