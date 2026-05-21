using System.Security.Cryptography;
using BooksApi.Common;
using BooksApi.Configuration;
using BooksApi.Data;
using BooksApi.Dtos;
using BooksApi.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace BooksApi.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<OtpSentResponse> ForgotPasswordAsync(string email);
    Task<ResetTokenResponse> VerifyOtpAsync(VerifyOtpRequest request);
    Task<OtpSentResponse> ResendOtpAsync(ResendOtpRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly MongoDbContext _db;
    private readonly ITokenService _tokens;
    private readonly JwtSettings _jwt;
    private readonly IWebHostEnvironment _env;
    private readonly bool _exposeOtpCode;

    public AuthService(MongoDbContext db, ITokenService tokens, IOptions<JwtSettings> jwt, IWebHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _tokens = tokens;
        _jwt = jwt.Value;
        _env = env;

        // OTP codes are exposed in the response when:
        //   - running in Development, OR
        //   - OTP_EXPOSE_CODE=true is set (used while no real email/SMS provider is wired up).
        var flag = Environment.GetEnvironmentVariable("OTP_EXPOSE_CODE");
        _exposeOtpCode = env.IsDevelopment()
                         || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (!request.AcceptTerms)
            throw AppException.BadRequest("İstifadə şərtlərini qəbul etməlisiniz.");

        var existing = await _db.Users.Find(u => u.Email == request.Email.ToLower()).FirstOrDefaultAsync();
        if (existing != null) throw AppException.Conflict("Bu e-poçt artıq qeydiyyatdan keçib.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _db.Users.InsertOneAsync(user);
        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync()
                   ?? throw AppException.Unauthorized("E-poçt və ya şifrə yanlışdır.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw AppException.Unauthorized("E-poçt və ya şifrə yanlışdır.");

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var hash = _tokens.HashToken(refreshToken);
        var token = await _db.RefreshTokens.Find(t => t.TokenHash == hash).FirstOrDefaultAsync()
                    ?? throw AppException.Unauthorized("Refresh token tapılmadı.");

        if (token.Revoked || token.ExpiresAt < DateTime.UtcNow)
            throw AppException.Unauthorized("Refresh token vaxtı bitib.");

        var user = await _db.Users.Find(u => u.Id == token.UserId).FirstOrDefaultAsync()
                   ?? throw AppException.Unauthorized();

        await _db.RefreshTokens.UpdateOneAsync(
            t => t.Id == token.Id,
            Builders<RefreshToken>.Update.Set(t => t.Revoked, true));

        return await BuildAuthResponseAsync(user);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = _tokens.HashToken(refreshToken);
        await _db.RefreshTokens.UpdateOneAsync(
            t => t.TokenHash == hash,
            Builders<RefreshToken>.Update.Set(t => t.Revoked, true));
    }

    public async Task<OtpSentResponse> ForgotPasswordAsync(string email)
    {
        email = email.Trim().ToLower();
        var user = await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync();

        // Always return success to prevent email enumeration; but if user exists, create OTP.
        var code = RandomNumberGenerator.GetInt32(1000, 10000).ToString();
        var expiresInSeconds = 120;
        var otp = new OtpCode
        {
            Target = email,
            CodeHash = _tokens.HashToken(code),
            Purpose = OtpPurpose.PasswordReset,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds)
        };

        if (user != null) await _db.OtpCodes.InsertOneAsync(otp);

        return new OtpSentResponse
        {
            Target = email,
            ExpiresInSeconds = expiresInSeconds,
            DevCode = _exposeOtpCode && user != null ? code : null
        };
    }

    public async Task<ResetTokenResponse> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var purpose = Enum.Parse<OtpPurpose>(request.Purpose);
        var target = request.Target.Trim().ToLower();
        var codeHash = _tokens.HashToken(request.Code);

        var otp = await _db.OtpCodes.Find(o =>
            o.Target == target &&
            o.Purpose == purpose &&
            !o.Used &&
            o.ExpiresAt > DateTime.UtcNow).SortByDescending(o => o.CreatedAt).FirstOrDefaultAsync()
            ?? throw AppException.BadRequest("OTP tapılmadı və ya vaxtı bitib.");

        if (otp.CodeHash != codeHash)
        {
            await _db.OtpCodes.UpdateOneAsync(o => o.Id == otp.Id,
                Builders<OtpCode>.Update.Inc(o => o.Attempts, 1));
            throw AppException.BadRequest("Daxil etdiyiniz kod yanlışdır.");
        }

        await _db.OtpCodes.UpdateOneAsync(o => o.Id == otp.Id,
            Builders<OtpCode>.Update.Set(o => o.Used, true));

        // Issue a short-lived reset token (HMAC of email+timestamp)
        var resetToken = _tokens.GenerateRefreshToken();
        var resetExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.OtpCodes.InsertOneAsync(new OtpCode
        {
            Target = target,
            CodeHash = _tokens.HashToken(resetToken),
            Purpose = OtpPurpose.PasswordReset,
            ExpiresAt = resetExpiry,
            Used = false
        });

        return new ResetTokenResponse { ResetToken = resetToken, ExpiresInSeconds = 600 };
    }

    public async Task<OtpSentResponse> ResendOtpAsync(ResendOtpRequest request)
    {
        return await ForgotPasswordAsync(request.Target);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var tokenHash = _tokens.HashToken(request.ResetToken);

        var otp = await _db.OtpCodes.Find(o =>
            o.Target == email &&
            o.CodeHash == tokenHash &&
            !o.Used &&
            o.ExpiresAt > DateTime.UtcNow).FirstOrDefaultAsync()
            ?? throw AppException.BadRequest("Reset token etibarsızdır.");

        await _db.OtpCodes.UpdateOneAsync(o => o.Id == otp.Id,
            Builders<OtpCode>.Update.Set(o => o.Used, true));

        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.Users.UpdateOneAsync(u => u.Email == email,
            Builders<User>.Update
                .Set(u => u.PasswordHash, hash)
                .Set(u => u.UpdatedAt, DateTime.UtcNow));
    }

    public Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request)
    {
        // Stub: integrate real provider verification later.
        throw AppException.BadRequest("Sosial login hələ aktiv deyil.");
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var access = _tokens.GenerateAccessToken(user, out var expiresAt);
        var refresh = _tokens.GenerateRefreshToken();
        await _db.RefreshTokens.InsertOneAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashToken(refresh),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        return new AuthResponse
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresAt = expiresAt,
            User = new UserShortDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role
            }
        };
    }
}
