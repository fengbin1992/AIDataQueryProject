namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 配置查询实体
/// </summary>
public class ConfigQuery
{
    public int Id { get; set; }

    /// <summary>
    /// 配置查询名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述说明
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// SQL 模板（含参数占位符）
    /// </summary>
    public string SqlContent { get; set; } = string.Empty;

    /// <summary>
    /// 默认数据库连接ID（可选）
    /// </summary>
    public int? ConnectionId { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public DatabaseConnection? Connection { get; set; }
    public User? Creator { get; set; }
    public ICollection<ConfigQueryParameter> Parameters { get; set; } = new List<ConfigQueryParameter>();
    public ICollection<ConfigQueryParamPreset> Presets { get; set; } = new List<ConfigQueryParamPreset>();
}
