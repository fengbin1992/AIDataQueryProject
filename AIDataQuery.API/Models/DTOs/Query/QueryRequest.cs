using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Query;

public class QueryRequest
{
    [Required(ErrorMessage = "平台编码不能为空")]
    public string PlatformCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "数据库连接ID不能为空")]
    public int ConnectionId { get; set; }

    [Required(ErrorMessage = "SQL语句不能为空")]
    public string Sql { get; set; } = string.Empty;
}
