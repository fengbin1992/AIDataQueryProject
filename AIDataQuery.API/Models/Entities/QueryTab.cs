namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 用户查询标签页实体
/// </summary>
public class QueryTab
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PlatformCode { get; set; }
    public int? ConnectionId { get; set; }
    public string? SqlContent { get; set; }
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
}
