using BooksApi.Common;
using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly ICartService _cart;
    private readonly ICurrentUserService _user;

    public OrdersController(IOrderService orders, ICartService cart, ICurrentUserService user)
    {
        _orders = orders;
        _cart = cart;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> List([FromQuery] OrderQuery query) =>
        Ok(await _orders.ListAsync(_user.RequireUserId(), query));

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> Get(string id) =>
        Ok(await _orders.GetAsync(_user.RequireUserId(), id));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request) =>
        Ok(await _orders.CreateFromCartAsync(_user.RequireUserId(), request));

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(string id) =>
        Ok(await _orders.CancelAsync(_user.RequireUserId(), id));

    [HttpPost("{id}/repeat")]
    public async Task<ActionResult<CartDto>> Repeat(string id) =>
        Ok(await _orders.RepeatAsync(_user.RequireUserId(), id, _cart));

    [HttpGet("{id}/track")]
    public async Task<ActionResult<OrderTrackingDto>> Track(string id) =>
        Ok(await _orders.GetTrackingAsync(_user.RequireUserId(), id));

    [HttpPut("{id}/status"), Authorize(Roles = "admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request) =>
        Ok(await _orders.UpdateStatusAsync(id, request.Status));
}
