using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/payment-cards")]
public class PaymentCardsController : ControllerBase
{
    private readonly IPaymentCardService _cards;
    private readonly ICurrentUserService _user;

    public PaymentCardsController(IPaymentCardService cards, ICurrentUserService user)
    {
        _cards = cards;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentCardDto>>> List() =>
        Ok(await _cards.ListAsync(_user.RequireUserId()));

    [HttpPost]
    public async Task<ActionResult<PaymentCardDto>> Create([FromBody] CreatePaymentCardRequest request) =>
        Ok(await _cards.CreateAsync(_user.RequireUserId(), request));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _cards.DeleteAsync(_user.RequireUserId(), id);
        return NoContent();
    }

    [HttpPut("{id}/set-default")]
    public async Task<ActionResult<PaymentCardDto>> SetDefault(string id) =>
        Ok(await _cards.SetDefaultAsync(_user.RequireUserId(), id));
}
