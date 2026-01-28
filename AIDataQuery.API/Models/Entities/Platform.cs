namespace AIDataQuery.API.Models.Entities;

public class Platform
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<DatabaseConnection> Connections { get; set; } = new List<DatabaseConnection>();
    public ICollection<UserPlatformPermission> UserPermissions { get; set; } = new List<UserPlatformPermission>();
}
