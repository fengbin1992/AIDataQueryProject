namespace AIDataQuery.API.Models.Entities;

public class DatabaseConnection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = "SqlServer";
    public string? Description { get; set; }
    /// <summary>
    /// 是否为正式环境数据库
    /// </summary>
    public bool IsProduction { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Platform? Platform { get; set; }
    public ICollection<UserConnectionPermission> UserPermissions { get; set; } = new List<UserConnectionPermission>();
}
