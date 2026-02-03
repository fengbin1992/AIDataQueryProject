using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 敏感数据脱敏规则（全局模式匹配）
/// </summary>
public class SensitiveMaskingRule
{
    public int Id { get; set; }

    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 字段名匹配模式，支持通配符，多个用逗号分隔
    /// 例如: *phone*,*mobile*,*tel*
    /// </summary>
    public string FieldPattern { get; set; } = string.Empty;

    /// <summary>
    /// 脱敏类型
    /// </summary>
    public MaskType MaskType { get; set; }

    /// <summary>
    /// 自定义脱敏配置（JSON格式，用于Custom类型）
    /// </summary>
    public string? MaskConfig { get; set; }

    /// <summary>
    /// 优先级，数字越大优先级越高
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 规则说明
    /// </summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
