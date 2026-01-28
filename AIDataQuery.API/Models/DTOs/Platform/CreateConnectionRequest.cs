using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Platform;

public class CreateConnectionRequest
{
    [Required(ErrorMessage = "连接名称不能为空")]
    [StringLength(100, ErrorMessage = "连接名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "平台编码不能为空")]
    public string PlatformCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "连接字符串不能为空")]
    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseType { get; set; } = "SqlServer";

    [StringLength(500, ErrorMessage = "描述不能超过500个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否为正式环境数据库
    /// </summary>
    public bool IsProduction { get; set; } = false;
}
