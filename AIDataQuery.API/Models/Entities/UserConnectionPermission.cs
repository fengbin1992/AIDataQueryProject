namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 用户数据库连接权限关联表
/// 用于细粒度控制用户可以访问的数据库连接
/// </summary>
public class UserConnectionPermission
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ConnectionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public DatabaseConnection? Connection { get; set; }
}
