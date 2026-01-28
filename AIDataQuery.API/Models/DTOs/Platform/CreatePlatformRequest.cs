using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Platform;

/// <summary>
/// 创建平台请求
/// </summary>
public class CreatePlatformRequest
{
    /// <summary>
    /// 平台编码（唯一标识）
    /// </summary>
    [Required(ErrorMessage = "平台编码不能为空")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "平台编码长度必须在2-50之间")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_-]*$", ErrorMessage = "平台编码必须以字母开头，只能包含字母、数字、下划线和连字符")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 平台名称
    /// </summary>
    [Required(ErrorMessage = "平台名称不能为空")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "平台名称长度必须在2-100之间")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 平台描述
    /// </summary>
    [StringLength(500, ErrorMessage = "描述长度不能超过500")]
    public string? Description { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;
}
