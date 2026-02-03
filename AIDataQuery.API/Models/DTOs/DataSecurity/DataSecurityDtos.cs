using AIDataQuery.API.Models.Enums;

namespace AIDataQuery.API.Models.DTOs.DataSecurity;

// ==================== 脱敏规则 DTOs ====================

public class MaskingRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FieldPattern { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string MaskTypeName => MaskType.ToString();
    public string? MaskConfig { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMaskingRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string FieldPattern { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string? MaskConfig { get; set; }
    public int Priority { get; set; } = 0;
    public string? Description { get; set; }
}

public class UpdateMaskingRuleRequest
{
    public string Name { get; set; } = string.Empty;
    public string FieldPattern { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string? MaskConfig { get; set; }
    public int Priority { get; set; }
    public string? Description { get; set; }
}

// ==================== 敏感字段标记 DTOs ====================

public class SensitiveFieldMarkDto
{
    public int Id { get; set; }
    public int ConnectionId { get; set; }
    public string ConnectionName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string MaskTypeName => MaskType.ToString();
    public string? MaskConfig { get; set; }
    public string? Description { get; set; }
    public int MarkedBy { get; set; }
    public string MarkedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateSensitiveFieldMarkRequest
{
    public int ConnectionId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string? MaskConfig { get; set; }
    public string? Description { get; set; }
}

public class BatchCreateSensitiveFieldMarksRequest
{
    public int ConnectionId { get; set; }
    public List<SensitiveFieldMarkItem> Fields { get; set; } = new();
}

public class SensitiveFieldMarkItem
{
    public string TableName { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// 表结构中的字段信息（包含敏感字段识别结果）
/// </summary>
public class FieldSchemaDto
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public string? MatchedRule { get; set; }
    public MaskType? MaskType { get; set; }
    public bool IsManuallyMarked { get; set; }
}

public class TableSchemaDto
{
    public string TableName { get; set; } = string.Empty;
    public List<FieldSchemaDto> Fields { get; set; } = new();
}

// ==================== 脱敏结果 DTOs ====================

public class SensitiveFieldInfo
{
    public string FieldName { get; set; } = string.Empty;
    public MaskType MaskType { get; set; }
    public string? MaskConfig { get; set; }
    public string Source { get; set; } = string.Empty; // "rule" or "manual"
    public string? RuleName { get; set; }
}

public class MaskedQueryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int ExecutionTimeMs { get; set; }
    public List<string> MaskedFields { get; set; } = new();
    public bool HasMaskedData => MaskedFields.Count > 0;
}
