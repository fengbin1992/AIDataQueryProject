using AIDataQuery.API.Models.DTOs.DataSecurity;
using AIDataQuery.API.Models.DTOs.Query;

namespace AIDataQuery.API.Services.Interfaces;

/// <summary>
/// 数据脱敏服务接口
/// </summary>
public interface IDataMaskingService
{
    // ==================== 脱敏规则管理 ====================

    Task<List<MaskingRuleDto>> GetMaskingRulesAsync();
    Task<MaskingRuleDto?> GetMaskingRuleAsync(int id);
    Task<MaskingRuleDto> CreateMaskingRuleAsync(CreateMaskingRuleRequest request);
    Task<MaskingRuleDto?> UpdateMaskingRuleAsync(int id, UpdateMaskingRuleRequest request);
    Task<bool> DeleteMaskingRuleAsync(int id);
    Task<bool> ToggleMaskingRuleAsync(int id);

    // ==================== 敏感字段标记 ====================

    Task<List<SensitiveFieldMarkDto>> GetSensitiveFieldMarksAsync(int? connectionId = null);
    Task<SensitiveFieldMarkDto> CreateSensitiveFieldMarkAsync(int userId, CreateSensitiveFieldMarkRequest request);
    Task<int> BatchCreateSensitiveFieldMarksAsync(int userId, BatchCreateSensitiveFieldMarksRequest request);
    Task<bool> DeleteSensitiveFieldMarkAsync(int id);
    Task<TableSchemaDto> GetTableSchemaWithSensitivityAsync(int connectionId, string tableName);

    // ==================== 脱敏处理 ====================

    /// <summary>
    /// 获取字段列表中的敏感字段
    /// </summary>
    Task<List<SensitiveFieldInfo>> GetSensitiveFieldsAsync(int connectionId, List<string> fieldNames);

    /// <summary>
    /// 对查询结果进行脱敏处理（自动脱敏所有敏感字段）
    /// </summary>
    Task<MaskedQueryResult> MaskQueryResultAsync(QueryResult result, int connectionId);

    /// <summary>
    /// 对单个值进行脱敏
    /// </summary>
    string MaskValue(string? value, SensitiveFieldInfo fieldInfo);
}
