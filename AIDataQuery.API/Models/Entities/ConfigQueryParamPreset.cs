namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 配置查询参数预设实体
/// </summary>
public class ConfigQueryParamPreset
{
    public int Id { get; set; }

    /// <summary>
    /// 所属配置查询ID
    /// </summary>
    public int ConfigQueryId { get; set; }

    /// <summary>
    /// 预设名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 参数值 JSON
    /// </summary>
    public string ParamValues { get; set; } = "{}";

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 是否为默认预设
    /// </summary>
    public bool IsDefault { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ConfigQuery? ConfigQuery { get; set; }
    public User? Creator { get; set; }
}
