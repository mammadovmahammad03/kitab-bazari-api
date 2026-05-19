using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/addresses")]
public class AddressesController : ControllerBase
{
    private readonly IAddressService _addresses;
    private readonly ICurrentUserService _user;

    public AddressesController(IAddressService addresses, ICurrentUserService user)
    {
        _addresses = addresses;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<List<AddressDto>>> List() =>
        Ok(await _addresses.ListAsync(_user.RequireUserId()));

    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create([FromBody] CreateAddressRequest request) =>
        Ok(await _addresses.CreateAsync(_user.RequireUserId(), request));

    [HttpPut("{id}")]
    public async Task<ActionResult<AddressDto>> Update(string id, [FromBody] UpdateAddressRequest request) =>
        Ok(await _addresses.UpdateAsync(_user.RequireUserId(), id, request));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _addresses.DeleteAsync(_user.RequireUserId(), id);
        return NoContent();
    }

    [HttpPut("{id}/set-default")]
    public async Task<ActionResult<AddressDto>> SetDefault(string id) =>
        Ok(await _addresses.SetDefaultAsync(_user.RequireUserId(), id));
}
