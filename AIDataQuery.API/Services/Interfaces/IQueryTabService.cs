using AIDataQuery.API.Models.DTOs.QueryTab;

namespace AIDataQuery.API.Services.Interfaces;

/// <summary>
/// 查询标签页服务接口
/// </summary>
public interface IQueryTabService
{
    /// <summary>
    /// 获取用户所有标签页
    /// </summary>
    Task<List<QueryTabDto>> GetUserTabsAsync(int userId);

    /// <summary>
    /// 获取单个标签页
    /// </summary>
    Task<QueryTabDto?> GetTabByIdAsync(int id, int userId);

    /// <summary>
    /// 创建标签页
    /// </summary>
    Task<QueryTabDto> CreateTabAsync(int userId, CreateQueryTabRequest request);

    /// <summary>
    /// 更新标签页
    /// </summary>
    Task<QueryTabDto?> UpdateTabAsync(int id, int userId, UpdateQueryTabRequest request);

    /// <summary>
    /// 删除标签页
    /// </summary>
    Task<bool> DeleteTabAsync(int id, int userId);

    /// <summary>
    /// 调整标签页排序
    /// </summary>
    Task<bool> ReorderTabsAsync(int userId, ReorderQueryTabsRequest request);
}
