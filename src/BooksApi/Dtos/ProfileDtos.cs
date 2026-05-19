using System.ComponentModel.DataAnnotations;

namespace BooksApi.Dtos;

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailVerified { get; set; }
    public bool PhoneVerified { get; set; }
    public ProfileStatsDto Stats { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ProfileStatsDto
{
    public int BooksPurchased { get; set; }
    public int FavoritesCount { get; set; }
    public int OrdersCount { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(6)] public string NewPassword { get; set; } = string.Empty;
}

public class UserSettingsDto
{
    public bool NotificationsEnabled { get; set; } = true;
    public string Language { get; set; } = "az";
    public bool DarkMode { get; set; }
}
