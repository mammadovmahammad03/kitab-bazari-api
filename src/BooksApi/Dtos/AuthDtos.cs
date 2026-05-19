using System.ComponentModel.DataAnnotations;

namespace BooksApi.Dtos;

public class RegisterRequest
{
    [Required, MinLength(2)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class SocialLoginRequest
{
    [Required] public string Provider { get; set; } = string.Empty; // google | facebook
    [Required] public string IdToken { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required] public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    [Required] public string Target { get; set; } = string.Empty; // email or phone
    [Required] public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = "PasswordReset"; // PasswordReset | Register | PhoneVerify
}

public class ResendOtpRequest
{
    [Required] public string Target { get; set; } = string.Empty;
    public string Purpose { get; set; } = "PasswordReset";
}

public class ResetPasswordRequest
{
    [Required] public string Email { get; set; } = string.Empty;
    [Required] public string ResetToken { get; set; } = string.Empty;
    [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserShortDto User { get; set; } = new();
}

public class UserShortDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "user";
}

public class OtpSentResponse
{
    public string Target { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public string? DevCode { get; set; } // only in development
}

public class ResetTokenResponse
{
    public string ResetToken { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
