using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 敏感字段标记（手动标记特定字段）
/// </summary>
public class SensitiveFieldMark
{
    public int Id { get; set; }

    /// <summary>
    /// 数据库连接ID
    /// </summary>
    public int ConnectionId { get; set; }

    /// <summary>
    /// 表名，支持通配符 * 表示所有表
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 脱敏类型
    /// </summary>
    public MaskType MaskType { get; set; }

    /// <summary>
    /// 自定义脱敏配置
    /// </summary>
    public string? MaskConfig { get; set; }

    /// <summary>
    /// 标记说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 标记人ID
    /// </summary>
    public int MarkedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public DatabaseConnection Connection { get; set; } = null!;
    public User Marker { get; set; } = null!;
}
