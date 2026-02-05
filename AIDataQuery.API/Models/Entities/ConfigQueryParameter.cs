namespace AIDataQuery.API.Models.Entities;

/// <summary>
/// 配置查询参数实体
/// </summary>
public class ConfigQueryParameter
{
    public int Id { get; set; }

    /// <summary>
    /// 所属配置查询ID
    /// </summary>
    public int ConfigQueryId { get; set; }

    /// <summary>
    /// 参数名（@paramName 中的 paramName）
    /// </summary>
    public string ParamName { get; set; } = string.Empty;

    /// <summary>
    /// 参数显示标签
    /// </summary>
    public string ParamLabel { get; set; } = string.Empty;

    /// <summary>
    /// 参数类型：text, number, date, daterange, select, multiselect
    /// </summary>
    public string ParamType { get; set; } = "text";

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 输入框占位提示
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// 下拉选项配置（JSON）
    /// </summary>
    public string? OptionsConfig { get; set; }

    /// <summary>
    /// 校验规则（正则表达式）
    /// </summary>
    public string? ValidationRule { get; set; }

    /// <summary>
    /// 扩展配置（JSON，如数字范围、日期格式等）
    /// </summary>
    public string? ExtraConfig { get; set; }

    /// <summary>
    /// 参数排序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 条件组名称（同组参数共享开关，如日期范围的StartDate和EndDate）
    /// 为空表示独立参数，使用ParamName作为条件标识
    /// </summary>
    public string? ConditionGroup { get; set; }

    // Navigation property
    public ConfigQuery? ConfigQuery { get; set; }
}
