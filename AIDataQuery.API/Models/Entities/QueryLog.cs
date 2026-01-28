using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.Entities;

public class QueryLog
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string? PlatformCode { get; set; }
    public string? DatabaseName { get; set; }
    public string SqlContent { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public QueryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
}
