using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.DTOs.QueryLog;

public class QueryLogDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? PlatformCode { get; set; }
    public string? DatabaseName { get; set; }
    public string SqlContent { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public QueryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; }
}
