namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 配置查询文件夹实体
/// </summary>
public class ConfigQueryFolder
{
    public int Id { get; set; }

    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 创建者ID
    /// </summary>
    public int CreatedBy { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Creator { get; set; }
    public ICollection<ConfigQuery> ConfigQueries { get; set; } = new List<ConfigQuery>();
}
