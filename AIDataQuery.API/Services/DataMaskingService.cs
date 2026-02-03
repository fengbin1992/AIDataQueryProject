using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.DataSecurity;
using AIDataQuery.API.Models.DTOs.Query;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Models.Enums;
using AIDataQuery.API.Services.Interfaces;
using AIDataQuery.API.Infrastructure.Encryption;

namespace AIDataQuery.API.Services;

public class DataMaskingService : IDataMaskingService
{
    private readonly AppDbContext _context;
    private readonly IAesEncryptor _aesEncryptor;
    private readonly ILogger<DataMaskingService> _logger;

    public DataMaskingService(
        AppDbContext context,
        IAesEncryptor aesEncryptor,
        ILogger<DataMaskingService> logger)
    {
        _context = context;
        _aesEncryptor = aesEncryptor;
        _logger = logger;
    }

    // ==================== 脱敏规则管理 ====================

    public async Task<List<MaskingRuleDto>> GetMaskingRulesAsync()
    {
        return await _context.SensitiveMaskingRules
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .Select(r => new MaskingRuleDto
            {
                Id = r.Id,
                Name = r.Name,
                FieldPattern = r.FieldPattern,
                MaskType = r.MaskType,
                MaskConfig = r.MaskConfig,
                Priority = r.Priority,
                IsActive = r.IsActive,
                Description = r.Description,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MaskingRuleDto?> GetMaskingRuleAsync(int id)
    {
        var rule = await _context.SensitiveMaskingRules.FindAsync(id);
        if (rule == null) return null;

        return new MaskingRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            FieldPattern = rule.FieldPattern,
            MaskType = rule.MaskType,
            MaskConfig = rule.MaskConfig,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            Description = rule.Description,
            CreatedAt = rule.CreatedAt
        };
    }

    public async Task<MaskingRuleDto> CreateMaskingRuleAsync(CreateMaskingRuleRequest request)
    {
        var rule = new SensitiveMaskingRule
        {
            Name = request.Name,
            FieldPattern = request.FieldPattern,
            MaskType = request.MaskType,
            MaskConfig = request.MaskConfig,
            Priority = request.Priority,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.SensitiveMaskingRules.Add(rule);
        await _context.SaveChangesAsync();

        return new MaskingRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            FieldPattern = rule.FieldPattern,
            MaskType = rule.MaskType,
            MaskConfig = rule.MaskConfig,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            Description = rule.Description,
            CreatedAt = rule.CreatedAt
        };
    }

    public async Task<MaskingRuleDto?> UpdateMaskingRuleAsync(int id, UpdateMaskingRuleRequest request)
    {
        var rule = await _context.SensitiveMaskingRules.FindAsync(id);
        if (rule == null) return null;

        rule.Name = request.Name;
        rule.FieldPattern = request.FieldPattern;
        rule.MaskType = request.MaskType;
        rule.MaskConfig = request.MaskConfig;
        rule.Priority = request.Priority;
        rule.Description = request.Description;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new MaskingRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            FieldPattern = rule.FieldPattern,
            MaskType = rule.MaskType,
            MaskConfig = rule.MaskConfig,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            Description = rule.Description,
            CreatedAt = rule.CreatedAt
        };
    }

    public async Task<bool> DeleteMaskingRuleAsync(int id)
    {
        var rule = await _context.SensitiveMaskingRules.FindAsync(id);
        if (rule == null) return false;

        _context.SensitiveMaskingRules.Remove(rule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleMaskingRuleAsync(int id)
    {
        var rule = await _context.SensitiveMaskingRules.FindAsync(id);
        if (rule == null) return false;

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== 敏感字段标记 ====================

    public async Task<List<SensitiveFieldMarkDto>> GetSensitiveFieldMarksAsync(int? connectionId = null)
    {
        var query = _context.SensitiveFieldMarks
            .Include(m => m.Connection)
            .Include(m => m.Marker)
            .AsQueryable();

        if (connectionId.HasValue)
        {
            query = query.Where(m => m.ConnectionId == connectionId.Value);
        }

        return await query
            .OrderBy(m => m.ConnectionId)
            .ThenBy(m => m.TableName)
            .ThenBy(m => m.FieldName)
            .Select(m => new SensitiveFieldMarkDto
            {
                Id = m.Id,
                ConnectionId = m.ConnectionId,
                ConnectionName = m.Connection.Name,
                TableName = m.TableName,
                FieldName = m.FieldName,
                MaskType = m.MaskType,
                MaskConfig = m.MaskConfig,
                Description = m.Description,
                MarkedBy = m.MarkedBy,
                MarkedByName = m.Marker.Nickname,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SensitiveFieldMarkDto> CreateSensitiveFieldMarkAsync(int userId, CreateSensitiveFieldMarkRequest request)
    {
        // 检查是否已存在
        var exists = await _context.SensitiveFieldMarks
            .AnyAsync(m => m.ConnectionId == request.ConnectionId &&
                          m.TableName == request.TableName &&
                          m.FieldName == request.FieldName);

        if (exists)
        {
            throw new InvalidOperationException("该字段已被标记");
        }

        var mark = new SensitiveFieldMark
        {
            ConnectionId = request.ConnectionId,
            TableName = request.TableName,
            FieldName = request.FieldName,
            MaskType = request.MaskType,
            MaskConfig = request.MaskConfig,
            Description = request.Description,
            MarkedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.SensitiveFieldMarks.Add(mark);
        await _context.SaveChangesAsync();

        var connection = await _context.DatabaseConnections.FindAsync(request.ConnectionId);
        var user = await _context.Users.FindAsync(userId);

        return new SensitiveFieldMarkDto
        {
            Id = mark.Id,
            ConnectionId = mark.ConnectionId,
            ConnectionName = connection?.Name ?? "",
            TableName = mark.TableName,
            FieldName = mark.FieldName,
            MaskType = mark.MaskType,
            MaskConfig = mark.MaskConfig,
            Description = mark.Description,
            MarkedBy = mark.MarkedBy,
            MarkedByName = user?.Nickname ?? "",
            CreatedAt = mark.CreatedAt
        };
    }

    public async Task<int> BatchCreateSensitiveFieldMarksAsync(int userId, BatchCreateSensitiveFieldMarksRequest request)
    {
        var existingMarks = await _context.SensitiveFieldMarks
            .Where(m => m.ConnectionId == request.ConnectionId)
            .Select(m => new { m.TableName, m.FieldName })
            .ToListAsync();

        var existingSet = existingMarks.Select(m => $"{m.TableName}.{m.FieldName}").ToHashSet();

        var newMarks = request.Fields
            .Where(f => !existingSet.Contains($"{f.TableName}.{f.FieldName}"))
            .Select(f => new SensitiveFieldMark
            {
                ConnectionId = request.ConnectionId,
                TableName = f.TableName,
                FieldName = f.FieldName,
                MaskType = f.MaskType,
                Description = f.Description,
                MarkedBy = userId,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (newMarks.Count > 0)
        {
            _context.SensitiveFieldMarks.AddRange(newMarks);
            await _context.SaveChangesAsync();
        }

        return newMarks.Count;
    }

    public async Task<bool> DeleteSensitiveFieldMarkAsync(int id)
    {
        var mark = await _context.SensitiveFieldMarks.FindAsync(id);
        if (mark == null) return false;

        _context.SensitiveFieldMarks.Remove(mark);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TableSchemaDto> GetTableSchemaWithSensitivityAsync(int connectionId, string tableName)
    {
        var connection = await _context.DatabaseConnections.FindAsync(connectionId);
        if (connection == null)
        {
            return new TableSchemaDto { TableName = tableName };
        }

        // 获取表结构
        var columns = await GetTableColumnsAsync(connection, tableName);

        // 获取脱敏规则
        var rules = await _context.SensitiveMaskingRules
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        // 获取手动标记
        var marks = await _context.SensitiveFieldMarks
            .Where(m => m.ConnectionId == connectionId &&
                       (m.TableName == tableName || m.TableName == "*"))
            .ToListAsync();

        var fields = new List<FieldSchemaDto>();
        foreach (var column in columns)
        {
            var field = new FieldSchemaDto
            {
                Name = column.Name,
                DataType = column.DataType
            };

            // 检查手动标记
            var mark = marks.FirstOrDefault(m =>
                m.FieldName.Equals(column.Name, StringComparison.OrdinalIgnoreCase));
            if (mark != null)
            {
                field.IsSensitive = true;
                field.IsManuallyMarked = true;
                field.MaskType = mark.MaskType;
                field.MatchedRule = "手动标记";
            }
            else
            {
                // 检查模式匹配规则
                foreach (var rule in rules)
                {
                    var patterns = rule.FieldPattern.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (patterns.Any(p => MatchPattern(column.Name, p.Trim())))
                    {
                        field.IsSensitive = true;
                        field.MaskType = rule.MaskType;
                        field.MatchedRule = rule.Name;
                        break;
                    }
                }
            }

            fields.Add(field);
        }

        return new TableSchemaDto
        {
            TableName = tableName,
            Fields = fields
        };
    }

    // ==================== 脱敏处理 ====================

    public async Task<List<SensitiveFieldInfo>> GetSensitiveFieldsAsync(int connectionId, List<string> fieldNames)
    {
        var result = new List<SensitiveFieldInfo>();

        // 获取全局脱敏规则
        var rules = await _context.SensitiveMaskingRules
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        // 获取手动标记的字段
        var marks = await _context.SensitiveFieldMarks
            .Where(m => m.ConnectionId == connectionId)
            .ToListAsync();

        foreach (var fieldName in fieldNames)
        {
            // 优先检查手动标记
            var mark = marks.FirstOrDefault(m =>
                m.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase) &&
                (m.TableName == "*" || true)); // 简化处理，实际应该匹配表名

            if (mark != null)
            {
                result.Add(new SensitiveFieldInfo
                {
                    FieldName = fieldName,
                    MaskType = mark.MaskType,
                    MaskConfig = mark.MaskConfig,
                    Source = "manual"
                });
                continue;
            }

            // 检查模式匹配规则
            foreach (var rule in rules)
            {
                var patterns = rule.FieldPattern.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (patterns.Any(p => MatchPattern(fieldName, p.Trim())))
                {
                    result.Add(new SensitiveFieldInfo
                    {
                        FieldName = fieldName,
                        MaskType = rule.MaskType,
                        MaskConfig = rule.MaskConfig,
                        Source = "rule",
                        RuleName = rule.Name
                    });
                    break;
                }
            }
        }

        return result;
    }

    public async Task<MaskedQueryResult> MaskQueryResultAsync(QueryResult result, int connectionId)
    {
        var maskedResult = new MaskedQueryResult
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Columns = result.Columns,
            TotalRows = result.TotalRows,
            ExecutionTimeMs = result.ExecutionTimeMs
        };

        if (!result.Success || result.Columns.Count == 0)
        {
            maskedResult.Rows = result.Rows;
            return maskedResult;
        }

        // 识别敏感字段
        var sensitiveFields = await GetSensitiveFieldsAsync(connectionId, result.Columns);

        if (sensitiveFields.Count == 0)
        {
            maskedResult.Rows = result.Rows;
            return maskedResult;
        }

        // 自动脱敏所有敏感字段
        var maskedRows = new List<Dictionary<string, object?>>();
        foreach (var row in result.Rows)
        {
            var maskedRow = new Dictionary<string, object?>(row);
            foreach (var field in sensitiveFields)
            {
                if (maskedRow.TryGetValue(field.FieldName, out var value))
                {
                    maskedRow[field.FieldName] = MaskValue(value?.ToString(), field);
                }
            }
            maskedRows.Add(maskedRow);
        }

        maskedResult.Rows = maskedRows;
        maskedResult.MaskedFields = sensitiveFields.Select(f => f.FieldName).ToList();

        return maskedResult;
    }

    public string MaskValue(string? value, SensitiveFieldInfo fieldInfo)
    {
        if (string.IsNullOrEmpty(value)) return value ?? "";

        return fieldInfo.MaskType switch
        {
            MaskType.Phone => MaskPhone(value),
            MaskType.IdCard => MaskIdCard(value),
            MaskType.Email => MaskEmail(value),
            MaskType.BankCard => MaskBankCard(value),
            MaskType.Name => MaskName(value),
            MaskType.Address => MaskAddress(value),
            MaskType.Amount => "******",
            MaskType.Full => "******",
            MaskType.Custom => MaskCustom(value, fieldInfo.MaskConfig),
            _ => MaskDefault(value)
        };
    }

    // ==================== 私有方法 ====================

    private bool MatchPattern(string fieldName, string pattern)
    {
        // 支持通配符匹配：*phone* 匹配 user_phone, phone_number 等
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(fieldName, regexPattern, RegexOptions.IgnoreCase);
    }

    private string MaskPhone(string value)
    {
        // 138****5678
        if (value.Length >= 11)
            return value.Substring(0, 3) + "****" + value.Substring(value.Length - 4);
        if (value.Length >= 7)
            return value.Substring(0, 3) + "****";
        return MaskDefault(value);
    }

    private string MaskIdCard(string value)
    {
        // 310***********1234
        if (value.Length >= 15)
            return value.Substring(0, 3) + new string('*', value.Length - 7) + value.Substring(value.Length - 4);
        return MaskDefault(value);
    }

    private string MaskEmail(string value)
    {
        // z***@example.com
        var atIndex = value.IndexOf('@');
        if (atIndex > 0)
            return value[0] + "***" + value.Substring(atIndex);
        return MaskDefault(value);
    }

    private string MaskBankCard(string value)
    {
        // 6222****0123
        if (value.Length >= 12)
            return value.Substring(0, 4) + new string('*', value.Length - 8) + value.Substring(value.Length - 4);
        return MaskDefault(value);
    }

    private string MaskName(string value)
    {
        // 张** 或 张*
        if (value.Length >= 2)
            return value[0] + new string('*', value.Length - 1);
        return "*";
    }

    private string MaskAddress(string value)
    {
        // 保留前6个字符，其余隐藏
        if (value.Length > 6)
            return value.Substring(0, 6) + "******";
        return MaskDefault(value);
    }

    private string MaskCustom(string value, string? config)
    {
        if (string.IsNullOrEmpty(config))
            return MaskDefault(value);

        try
        {
            var customConfig = JsonSerializer.Deserialize<CustomMaskConfig>(config);
            if (customConfig != null && !string.IsNullOrEmpty(customConfig.Pattern))
            {
                return Regex.Replace(value, customConfig.Pattern, customConfig.Replacement ?? "***");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply custom mask config: {Config}", config);
        }

        return MaskDefault(value);
    }

    private string MaskDefault(string value)
    {
        if (value.Length <= 2) return "**";
        return value[0] + new string('*', value.Length - 2) + value[value.Length - 1];
    }

    private async Task<List<ColumnInfo>> GetTableColumnsAsync(DatabaseConnection connection, string tableName)
    {
        try
        {
            var connectionString = _aesEncryptor.Decrypt(connection.ConnectionString);
            using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await sqlConnection.OpenAsync();

            var sql = @"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            using var command = new Microsoft.Data.SqlClient.SqlCommand(sql, sqlConnection);
            command.Parameters.AddWithValue("@TableName", tableName);

            using var reader = await command.ExecuteReaderAsync();

            var columns = new List<ColumnInfo>();
            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES"
                });
            }

            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get columns for table {TableName}", tableName);
            return new List<ColumnInfo>();
        }
    }

    private class CustomMaskConfig
    {
        public string? Pattern { get; set; }
        public string? Replacement { get; set; }
    }
}
