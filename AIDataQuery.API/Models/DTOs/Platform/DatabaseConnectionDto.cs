namespace AIDataQuery.API.Models.DTOs.Platform;

public class DatabaseConnectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlatformCode { get; set; } = string.Empty;
    public string DatabaseType { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>
    /// 是否为正式环境数据库
    /// </summary>
    public bool IsProduction { get; set; }
    public bool IsActive { get; set; }
    /// <summary>
    /// 连接字符串（仅管理员可见，普通用户查询时不返回）
    /// </summary>
    public string? ConnectionString { get; set; }
}
