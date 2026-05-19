using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    private readonly ICurrentUserService _user;

    public ReviewsController(IReviewService reviews, ICurrentUserService user)
    {
        _reviews = reviews;
        _user = user;
    }

    [HttpGet("book/{bookId}")]
    public async Task<ActionResult<List<ReviewDto>>> ListByBook(string bookId) =>
        Ok(await _reviews.ListByBookAsync(bookId));

    [HttpPost, Authorize]
    public async Task<ActionResult<ReviewDto>> Create([FromBody] CreateReviewRequest request) =>
        Ok(await _reviews.CreateAsync(_user.RequireUserId(), request));
}
