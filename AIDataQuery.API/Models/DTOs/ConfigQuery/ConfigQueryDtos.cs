using System.Text.Json.Serialization;

namespace AIDataQuery.API.Models.DTOs.ConfigQuery;

#region 配置查询 DTO

/// <summary>
/// 配置查询列表项 DTO
/// </summary>
public class ConfigQueryListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? FolderId { get; set; }
    public string? FolderName { get; set; }
}

/// <summary>
/// 配置查询详情 DTO
/// </summary>
public class ConfigQueryDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SqlContent { get; set; } = string.Empty;
    public int? ConnectionId { get; set; }
    public string? ConnectionName { get; set; }
    public bool IsPublic { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public bool CanEdit { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ConfigQueryParameterDto> Parameters { get; set; } = new();
}

/// <summary>
/// 配置查询参数 DTO
/// </summary>
public class ConfigQueryParameterDto
{
    public int Id { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public string ParamLabel { get; set; } = string.Empty;
    public string ParamType { get; set; } = "text";
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public OptionsConfigDto? OptionsConfig { get; set; }
    public string? ValidationRule { get; set; }
    public ExtraConfigDto? ExtraConfig { get; set; }
    public int SortOrder { get; set; }
}

#endregion

#region 选项配置 DTO

/// <summary>
/// 选项配置 DTO
/// </summary>
public class OptionsConfigDto
{
    /// <summary>
    /// 模式：static 或 dynamic
    /// </summary>
    public string Mode { get; set; } = "static";

    /// <summary>
    /// 静态选项列表
    /// </summary>
    public List<OptionItem>? Options { get; set; }

    /// <summary>
    /// 动态 SQL 查询
    /// </summary>
    public string? Sql { get; set; }

    /// <summary>
    /// 动态查询使用的连接ID
    /// </summary>
    public int? ConnectionId { get; set; }

    /// <summary>
    /// 是否在打开时刷新
    /// </summary>
    public bool? RefreshOnOpen { get; set; }
}

/// <summary>
/// 选项项
/// </summary>
public class OptionItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// 扩展配置 DTO
/// </summary>
public class ExtraConfigDto
{
    // 数字类型配置
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public int? Precision { get; set; }
    public decimal? Step { get; set; }

    // 日期类型配置
    public string? Format { get; set; }
    public string? DefaultType { get; set; }
    public string? CustomDefault { get; set; }

    // 日期范围配置
    public string? StartParamName { get; set; }
    public string? EndParamName { get; set; }

    // 多选配置
    public string? Separator { get; set; }
    public string? Wrapper { get; set; }
}

#endregion

#region 请求 DTO

/// <summary>
/// 创建配置查询请求
/// </summary>
public class CreateConfigQueryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SqlContent { get; set; } = string.Empty;
    public int? ConnectionId { get; set; }
    public bool IsPublic { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public int? FolderId { get; set; }
    public List<CreateConfigQueryParameterRequest> Parameters { get; set; } = new();
}

/// <summary>
/// 创建配置查询参数请求
/// </summary>
public class CreateConfigQueryParameterRequest
{
    public string ParamName { get; set; } = string.Empty;
    public string ParamLabel { get; set; } = string.Empty;
    public string ParamType { get; set; } = "text";
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public OptionsConfigDto? OptionsConfig { get; set; }
    public string? ValidationRule { get; set; }
    public ExtraConfigDto? ExtraConfig { get; set; }
    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// 更新配置查询请求
/// </summary>
public class UpdateConfigQueryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SqlContent { get; set; }
    public int? ConnectionId { get; set; }
    public bool? IsPublic { get; set; }
    public int? SortOrder { get; set; }
    public int? FolderId { get; set; }
    public bool ClearFolder { get; set; } = false;
    public List<CreateConfigQueryParameterRequest>? Parameters { get; set; }
}

/// <summary>
/// 执行配置查询请求
/// </summary>
public class ExecuteConfigQueryRequest
{
    /// <summary>
    /// 可覆盖默认连接
    /// </summary>
    public int? ConnectionId { get; set; }

    /// <summary>
    /// 参数值（JSON 对象）
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();
}

/// <summary>
/// 解析 SQL 参数请求
/// </summary>
public class ParseParamsRequest
{
    public string Sql { get; set; } = string.Empty;
}

/// <summary>
/// 解析 SQL 参数响应
/// </summary>
public class ParseParamsResponse
{
    public List<string> Parameters { get; set; } = new();
}

/// <summary>
/// 导入配置请求
/// </summary>
public class ImportConfigQueryRequest
{
    public string Json { get; set; } = string.Empty;
}

/// <summary>
/// 获取动态选项请求
/// </summary>
public class GetOptionsRequest
{
    public int ConnectionId { get; set; }
    public string Sql { get; set; } = string.Empty;
}

/// <summary>
/// 获取动态选项响应
/// </summary>
public class GetOptionsResponse
{
    public List<OptionItem> Options { get; set; } = new();
}

#endregion

#region 参数预设 DTO

/// <summary>
/// 参数预设 DTO
/// </summary>
public class ConfigQueryParamPresetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> ParamValues { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建参数预设请求
/// </summary>
public class CreateParamPresetRequest
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object?> ParamValues { get; set; } = new();
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// 更新参数预设请求
/// </summary>
public class UpdateParamPresetRequest
{
    public string? Name { get; set; }
    public Dictionary<string, object?>? ParamValues { get; set; }
    public bool? IsDefault { get; set; }
}

#endregion

#region 分页响应

/// <summary>
/// 分页列表响应
/// </summary>
public class PagedListResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

#endregion

#region 导出格式

/// <summary>
/// 配置查询导出格式
/// </summary>
public class ConfigQueryExportDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Sql { get; set; } = string.Empty;
    public int? ConnectionId { get; set; }
    public List<ConfigQueryParameterExportDto> Parameters { get; set; } = new();
}

/// <summary>
/// 配置查询参数导出格式
/// </summary>
public class ConfigQueryParameterExportDto
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public bool Required { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public OptionsConfigDto? Options { get; set; }
    public string? ValidationRule { get; set; }
    public ExtraConfigDto? ExtraConfig { get; set; }
}

#endregion

#region 文件夹 DTO

/// <summary>
/// 配置查询文件夹 DTO
/// </summary>
public class ConfigQueryFolderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建文件夹请求
/// </summary>
public class CreateConfigQueryFolderRequest
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// 更新文件夹请求
/// </summary>
public class UpdateConfigQueryFolderRequest
{
    public string? Name { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>
/// 移动配置查询到文件夹请求
/// </summary>
public class MoveToFolderRequest
{
    public int? FolderId { get; set; }
}

#endregion
