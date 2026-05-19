using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profile;
    private readonly ICurrentUserService _user;

    public ProfileController(IProfileService profile, ICurrentUserService user)
    {
        _profile = profile;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get() =>
        Ok(await _profile.GetAsync(_user.RequireUserId()));

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update([FromBody] UpdateProfileRequest request) =>
        Ok(await _profile.UpdateAsync(_user.RequireUserId(), request));

    [HttpPost("avatar")]
    public async Task<ActionResult<ProfileDto>> UpdateAvatar([FromBody] UpdateProfileRequest request) =>
        Ok(await _profile.UpdateAsync(_user.RequireUserId(), new UpdateProfileRequest { AvatarUrl = request.AvatarUrl }));

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _profile.ChangePasswordAsync(_user.RequireUserId(), request);
        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProfileStatsDto>> Stats() =>
        Ok(await _profile.GetStatsAsync(_user.RequireUserId()));

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        await _profile.DeleteAsync(_user.RequireUserId());
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IProfileService _profile;
    private readonly ICurrentUserService _user;

    public SettingsController(IProfileService profile, ICurrentUserService user)
    {
        _profile = profile;
        _user = user;
    }

    [HttpGet]
    public async Task<ActionResult<UserSettingsDto>> Get() =>
        Ok(await _profile.GetSettingsAsync(_user.RequireUserId()));

    [HttpPut]
    public async Task<ActionResult<UserSettingsDto>> Update([FromBody] UserSettingsDto request) =>
        Ok(await _profile.UpdateSettingsAsync(_user.RequireUserId(), request));
}
