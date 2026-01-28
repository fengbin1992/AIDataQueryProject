namespace AIDataQuery.API.Models.DTOs.Platform;

public class PlatformDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ConnectionCount { get; set; }
}
