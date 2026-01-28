using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string ThemePreference { get; set; } = "auto";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<UserPlatformPermission> PlatformPermissions { get; set; } = new List<UserPlatformPermission>();
    public ICollection<UserConnectionPermission> ConnectionPermissions { get; set; } = new List<UserConnectionPermission>();
    public ICollection<QueryTemplate> Templates { get; set; } = new List<QueryTemplate>();
    public ICollection<QueryLog> QueryLogs { get; set; } = new List<QueryLog>();
}
