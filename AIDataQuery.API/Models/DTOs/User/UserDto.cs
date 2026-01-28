using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.DTOs.User;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string ThemePreference { get; set; } = "auto";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> PlatformCodes { get; set; } = new();
    public List<int> ConnectionIds { get; set; } = new();
}
