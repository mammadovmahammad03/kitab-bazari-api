using BooksApi.Models;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Route("api/promo")]
public class PromoController : ControllerBase
{
    private readonly IPromoService _promo;

    public PromoController(IPromoService promo) => _promo = promo;

    [HttpGet("validate/{code}")]
    public async Task<IActionResult> Validate(string code, [FromQuery] decimal subtotal = 0) =>
        Ok(await _promo.ValidateAsync(code, subtotal));

    [HttpGet, Authorize(Roles = "admin")]
    public async Task<IActionResult> List() => Ok(await _promo.ListAsync());

    [HttpPost, Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] PromoCode body) => Ok(await _promo.CreateAsync(body));

    [HttpDelete("{id}"), Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(string id)
    {
        await _promo.DeleteAsync(id);
        return NoContent();
    }
}
