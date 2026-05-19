using BooksApi.Dtos;
using BooksApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BooksApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request) =>
        Ok(await _auth.RegisterAsync(request));

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) =>
        Ok(await _auth.LoginAsync(request));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request) =>
        Ok(await _auth.RefreshAsync(request.RefreshToken));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _auth.LogoutAsync(request.RefreshToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<OtpSentResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request) =>
        Ok(await _auth.ForgotPasswordAsync(request.Email));

    [HttpPost("verify-otp")]
    public async Task<ActionResult<ResetTokenResponse>> VerifyOtp([FromBody] VerifyOtpRequest request) =>
        Ok(await _auth.VerifyOtpAsync(request));

    [HttpPost("resend-otp")]
    public async Task<ActionResult<OtpSentResponse>> ResendOtp([FromBody] ResendOtpRequest request) =>
        Ok(await _auth.ResendOtpAsync(request));

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _auth.ResetPasswordAsync(request);
        return NoContent();
    }

    [HttpPost("social/{provider}")]
    public async Task<ActionResult<AuthResponse>> Social(string provider, [FromBody] SocialLoginRequest request)
    {
        request.Provider = provider;
        return Ok(await _auth.SocialLoginAsync(request));
    }
}
