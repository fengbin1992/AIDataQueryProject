using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Query;

public class ExportRequest
{
    [Required(ErrorMessage = "平台编码不能为空")]
    public string PlatformCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "数据库连接ID不能为空")]
    public int ConnectionId { get; set; }

    [Required(ErrorMessage = "SQL语句不能为空")]
    public string Sql { get; set; } = string.Empty;

    [Required(ErrorMessage = "导出格式不能为空")]
    public string Format { get; set; } = "csv"; // csv or excel
}
