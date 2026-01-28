namespace AIDataQuery.API.Models.DTOs.QueryTab;

/// <summary>
/// 查询标签页 DTO
/// </summary>
public class QueryTabDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PlatformCode { get; set; }
    public int? ConnectionId { get; set; }
    public string? SqlContent { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
