using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;
    private readonly ICurrentUserService _user;

    public CartController(ICartService cart, ICurrentUserService user)
    {
        _cart = cart;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get() => Ok(await _cart.GetAsync(_user.RequireUserId()));

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> Add([FromBody] AddCartItemRequest request) =>
        Ok(await _cart.AddItemAsync(_user.RequireUserId(), request));

    [HttpPut("items/{bookId}")]
    public async Task<ActionResult<CartDto>> Update(string bookId, [FromBody] UpdateCartItemRequest request) =>
        Ok(await _cart.UpdateItemAsync(_user.RequireUserId(), bookId, request.Quantity));

    [HttpDelete("items/{bookId}")]
    public async Task<ActionResult<CartDto>> Remove(string bookId) =>
        Ok(await _cart.RemoveItemAsync(_user.RequireUserId(), bookId));

    [HttpDelete]
    public async Task<ActionResult<CartDto>> Clear() =>
        Ok(await _cart.ClearAsync(_user.RequireUserId()));

    [HttpPost("apply-promo")]
    public async Task<ActionResult<CartDto>> ApplyPromo([FromBody] ApplyPromoRequest request) =>
        Ok(await _cart.ApplyPromoAsync(_user.RequireUserId(), request.Code));

    [HttpDelete("promo")]
    public async Task<ActionResult<CartDto>> RemovePromo() =>
        Ok(await _cart.RemovePromoAsync(_user.RequireUserId()));
}
