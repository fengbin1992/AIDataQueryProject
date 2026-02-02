using AIDataQuery.API.Models.DTOs.ConfigQuery;
using AIDataQuery.API.Models.DTOs.Query;

namespace AIDataQuery.API.Services.Interfaces;

/// <summary>
/// 配置查询服务接口
/// </summary>
public interface IConfigQueryService
{
    #region 配置查询 CRUD

    /// <summary>
    /// 获取配置查询列表
    /// </summary>
    Task<PagedListResponse<ConfigQueryListItemDto>> GetListAsync(
        int userId, bool isAdmin, string? keyword, int pageIndex, int pageSize);

    /// <summary>
    /// 获取配置查询详情
    /// </summary>
    Task<ConfigQueryDetailDto?> GetByIdAsync(int id, int userId, bool isAdmin);

    /// <summary>
    /// 创建配置查询
    /// </summary>
    Task<int> CreateAsync(int userId, bool isAdmin, CreateConfigQueryRequest request);

    /// <summary>
    /// 更新配置查询
    /// </summary>
    Task<bool> UpdateAsync(int id, int userId, bool isAdmin, UpdateConfigQueryRequest request);

    /// <summary>
    /// 删除配置查询
    /// </summary>
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    /// <summary>
    /// 复制配置查询
    /// </summary>
    Task<int> CopyAsync(int id, int userId);

    #endregion

    #region 执行相关

    /// <summary>
    /// 执行配置查询
    /// </summary>
    Task<QueryResult> ExecuteAsync(int id, int userId, bool isAdmin, ExecuteConfigQueryRequest request, string? clientIp);

    /// <summary>
    /// 解析 SQL 中的参数
    /// </summary>
    Task<ParseParamsResponse> ParseParamsAsync(string sql);

    /// <summary>
    /// 获取动态选项
    /// </summary>
    Task<GetOptionsResponse> GetOptionsAsync(int userId, bool isAdmin, GetOptionsRequest request);

    #endregion

    #region 导入导出

    /// <summary>
    /// 导入配置
    /// </summary>
    Task<int> ImportAsync(int userId, bool isAdmin, string json);

    /// <summary>
    /// 导出配置
    /// </summary>
    Task<ConfigQueryExportDto?> ExportAsync(int id, int userId, bool isAdmin);

    #endregion

    #region 参数预设

    /// <summary>
    /// 获取参数预设列表
    /// </summary>
    Task<List<ConfigQueryParamPresetDto>> GetPresetsAsync(int configQueryId, int userId);

    /// <summary>
    /// 创建参数预设
    /// </summary>
    Task<int> CreatePresetAsync(int configQueryId, int userId, CreateParamPresetRequest request);

    /// <summary>
    /// 更新参数预设
    /// </summary>
    Task<bool> UpdatePresetAsync(int configQueryId, int presetId, int userId, UpdateParamPresetRequest request);

    /// <summary>
    /// 删除参数预设
    /// </summary>
    Task<bool> DeletePresetAsync(int configQueryId, int presetId, int userId);

    #endregion
}
