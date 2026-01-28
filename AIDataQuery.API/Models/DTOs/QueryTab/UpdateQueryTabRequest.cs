using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.QueryTab;

/// <summary>
/// 更新查询标签页请求
/// </summary>
public class UpdateQueryTabRequest
{
    [StringLength(100, ErrorMessage = "标签名称不能超过100个字符")]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? PlatformCode { get; set; }

    public int? ConnectionId { get; set; }

    public string? SqlContent { get; set; }
}
