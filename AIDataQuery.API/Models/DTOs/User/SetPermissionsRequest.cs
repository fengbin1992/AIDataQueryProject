namespace AIDataQuery.API.Models.DTOs.User;

/// <summary>
/// 设置用户权限请求
/// </summary>
public class SetPermissionsRequest
{
    /// <summary>
    /// 平台编码列表
    /// </summary>
    public List<string> PlatformCodes { get; set; } = new();

    /// <summary>
    /// 数据库连接ID列表
    /// </summary>
    public List<int> ConnectionIds { get; set; } = new();
}
