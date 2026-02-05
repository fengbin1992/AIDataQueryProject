using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.ConfigQuery;
using AIDataQuery.API.Models.DTOs.Query;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Models.Enums;
using AIDataQuery.API.Services.Interfaces;
using AIDataQuery.API.Infrastructure.Encryption;

namespace AIDataQuery.API.Services;

public class ConfigQueryService : IConfigQueryService
{
    private readonly AppDbContext _context;
    private readonly IAesEncryptor _aesEncryptor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigQueryService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ConfigQueryService(
        AppDbContext context,
        IAesEncryptor aesEncryptor,
        IConfiguration configuration,
        ILogger<ConfigQueryService> logger)
    {
        _context = context;
        _aesEncryptor = aesEncryptor;
        _configuration = configuration;
        _logger = logger;
    }

    #region 配置查询 CRUD

    public async Task<PagedListResponse<ConfigQueryListItemDto>> GetListAsync(
        int userId, bool isAdmin, string? keyword, int pageIndex, int pageSize)
    {
        // 用户可见的配置查询 = 自己创建的 + 管理员公开的
        var query = _context.ConfigQueries
            .Include(q => q.Creator)
            .Include(q => q.Folder)
            .Where(q => q.IsActive)
            .Where(q => q.CreatedBy == userId || q.IsPublic);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(q => q.Name.Contains(keyword) ||
                (q.Description != null && q.Description.Contains(keyword)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(q => q.CreatedBy == userId) // 自己的排前面
            .ThenBy(q => q.SortOrder)
            .ThenByDescending(q => q.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new ConfigQueryListItemDto
            {
                Id = q.Id,
                Name = q.Name,
                Description = q.Description,
                IsPublic = q.IsPublic,
                CreatedBy = q.CreatedBy,
                CreatedByName = q.Creator != null ? q.Creator.Nickname : "",
                IsOwner = q.CreatedBy == userId,
                CanEdit = q.CreatedBy == userId || isAdmin,
                CreatedAt = q.CreatedAt,
                FolderId = q.FolderId,
                FolderName = q.Folder != null ? q.Folder.Name : null
            })
            .ToListAsync();

        return new PagedListResponse<ConfigQueryListItemDto>
        {
            Items = items,
            Total = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<ConfigQueryDetailDto?> GetByIdAsync(int id, int userId, bool isAdmin)
    {
        var query = await _context.ConfigQueries
            .Include(q => q.Creator)
            .Include(q => q.Connection)
            .Include(q => q.Parameters.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (query == null) return null;

        // 检查权限：只能查看自己的或公开的
        if (query.CreatedBy != userId && !query.IsPublic)
        {
            return null;
        }

        return MapToDetailDto(query, userId, isAdmin);
    }

    public async Task<int> CreateAsync(int userId, bool isAdmin, CreateConfigQueryRequest request)
    {
        var configQuery = new ConfigQuery
        {
            Name = request.Name,
            Description = request.Description,
            SqlContent = request.SqlContent,
            ConnectionId = request.ConnectionId,
            // 普通用户创建的配置查询强制为私有，只有管理员可以创建公开的
            IsPublic = isAdmin ? request.IsPublic : false,
            CreatedBy = userId,
            SortOrder = request.SortOrder,
            FolderId = request.FolderId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfigQueries.Add(configQuery);
        await _context.SaveChangesAsync();

        // 添加参数
        if (request.Parameters.Count > 0)
        {
            int sortOrder = 0;
            foreach (var param in request.Parameters)
            {
                var parameter = new ConfigQueryParameter
                {
                    ConfigQueryId = configQuery.Id,
                    ParamName = param.ParamName,
                    ParamLabel = param.ParamLabel,
                    ParamType = param.ParamType,
                    IsRequired = param.IsRequired,
                    DefaultValue = param.DefaultValue,
                    Placeholder = param.Placeholder,
                    OptionsConfig = param.OptionsConfig != null
                        ? JsonSerializer.Serialize(param.OptionsConfig, JsonOptions)
                        : null,
                    ValidationRule = param.ValidationRule,
                    ExtraConfig = param.ExtraConfig != null
                        ? JsonSerializer.Serialize(param.ExtraConfig, JsonOptions)
                        : null,
                    SortOrder = param.SortOrder > 0 ? param.SortOrder : sortOrder++,
                    ConditionGroup = param.ConditionGroup
                };
                _context.ConfigQueryParameters.Add(parameter);
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created config query {Id} by user {UserId}", configQuery.Id, userId);
        return configQuery.Id;
    }

    public async Task<bool> UpdateAsync(int id, int userId, bool isAdmin, UpdateConfigQueryRequest request)
    {
        var configQuery = await _context.ConfigQueries
            .Include(q => q.Parameters)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (configQuery == null) return false;

        // 检查权限：只有创建者或管理员可以编辑
        if (configQuery.CreatedBy != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("只能编辑自己创建的配置查询");
        }

        if (!string.IsNullOrEmpty(request.Name)) configQuery.Name = request.Name;
        if (request.Description != null) configQuery.Description = request.Description;
        if (!string.IsNullOrEmpty(request.SqlContent)) configQuery.SqlContent = request.SqlContent;
        if (request.ConnectionId.HasValue) configQuery.ConnectionId = request.ConnectionId.Value == 0 ? null : request.ConnectionId;
        // 只有管理员可以修改公开状态
        if (request.IsPublic.HasValue && isAdmin) configQuery.IsPublic = request.IsPublic.Value;
        if (request.SortOrder.HasValue) configQuery.SortOrder = request.SortOrder.Value;
        // 处理文件夹
        if (request.ClearFolder) configQuery.FolderId = null;
        else if (request.FolderId.HasValue) configQuery.FolderId = request.FolderId;
        configQuery.UpdatedAt = DateTime.UtcNow;

        // 更新参数
        if (request.Parameters != null)
        {
            // 删除旧参数
            _context.ConfigQueryParameters.RemoveRange(configQuery.Parameters);

            // 添加新参数
            int sortOrder = 0;
            foreach (var param in request.Parameters)
            {
                var parameter = new ConfigQueryParameter
                {
                    ConfigQueryId = configQuery.Id,
                    ParamName = param.ParamName,
                    ParamLabel = param.ParamLabel,
                    ParamType = param.ParamType,
                    IsRequired = param.IsRequired,
                    DefaultValue = param.DefaultValue,
                    Placeholder = param.Placeholder,
                    OptionsConfig = param.OptionsConfig != null
                        ? JsonSerializer.Serialize(param.OptionsConfig, JsonOptions)
                        : null,
                    ValidationRule = param.ValidationRule,
                    ExtraConfig = param.ExtraConfig != null
                        ? JsonSerializer.Serialize(param.ExtraConfig, JsonOptions)
                        : null,
                    SortOrder = param.SortOrder > 0 ? param.SortOrder : sortOrder++,
                    ConditionGroup = param.ConditionGroup
                };
                _context.ConfigQueryParameters.Add(parameter);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated config query {Id}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var configQuery = await _context.ConfigQueries
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (configQuery == null) return false;

        // 检查权限：只有创建者或管理员可以删除
        if (configQuery.CreatedBy != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("只能删除自己创建的配置查询");
        }

        // 软删除
        configQuery.IsActive = false;
        configQuery.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted config query {Id}", id);
        return true;
    }

    public async Task<int> CopyAsync(int id, int userId)
    {
        var original = await _context.ConfigQueries
            .Include(q => q.Parameters)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (original == null)
        {
            throw new InvalidOperationException("配置查询不存在");
        }

        // 检查权限：只能复制自己的或公开的
        if (original.CreatedBy != userId && !original.IsPublic)
        {
            throw new UnauthorizedAccessException("无权复制此配置查询");
        }

        var copy = new ConfigQuery
        {
            Name = $"{original.Name} (副本)",
            Description = original.Description,
            SqlContent = original.SqlContent,
            ConnectionId = original.ConnectionId,
            IsPublic = false, // 复制后默认为私有
            CreatedBy = userId,
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfigQueries.Add(copy);
        await _context.SaveChangesAsync();

        // 复制参数
        foreach (var param in original.Parameters)
        {
            var copyParam = new ConfigQueryParameter
            {
                ConfigQueryId = copy.Id,
                ParamName = param.ParamName,
                ParamLabel = param.ParamLabel,
                ParamType = param.ParamType,
                IsRequired = param.IsRequired,
                DefaultValue = param.DefaultValue,
                Placeholder = param.Placeholder,
                OptionsConfig = param.OptionsConfig,
                ValidationRule = param.ValidationRule,
                ExtraConfig = param.ExtraConfig,
                SortOrder = param.SortOrder,
                ConditionGroup = param.ConditionGroup
            };
            _context.ConfigQueryParameters.Add(copyParam);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Copied config query {OriginalId} to {CopyId} by user {UserId}", id, copy.Id, userId);
        return copy.Id;
    }

    #endregion

    #region 执行相关

    public async Task<QueryResult> ExecuteAsync(int id, int userId, bool isAdmin, ExecuteConfigQueryRequest request, string? clientIp)
    {
        var stopwatch = Stopwatch.StartNew();

        var configQuery = await _context.ConfigQueries
            .Include(q => q.Parameters)
            .Include(q => q.Connection)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (configQuery == null)
        {
            return new QueryResult { Success = false, ErrorMessage = "配置查询不存在" };
        }

        // 检查权限
        if (configQuery.CreatedBy != userId && !configQuery.IsPublic)
        {
            return new QueryResult { Success = false, ErrorMessage = "无权执行此配置查询" };
        }

        // 确定连接
        var connectionId = request.ConnectionId ?? configQuery.ConnectionId;
        if (!connectionId.HasValue)
        {
            return new QueryResult { Success = false, ErrorMessage = "未指定数据库连接" };
        }

        var connection = await _context.DatabaseConnections.FindAsync(connectionId.Value);
        if (connection == null)
        {
            return new QueryResult { Success = false, ErrorMessage = "数据库连接不存在" };
        }

        // 调试日志：输出前端传递的 enabledConditions
        _logger.LogInformation("EnabledConditions from request: {EnabledConditions}",
            request.EnabledConditions == null ? "null" : $"[{string.Join(", ", request.EnabledConditions)}]");

        // 验证必填参数（只验证启用的参数）
        var enabledSet = request.EnabledConditions != null
            ? new HashSet<string>(request.EnabledConditions, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var param in configQuery.Parameters.Where(p => p.IsRequired))
        {
            // 检查参数或其所属组是否启用
            var conditionKey = string.IsNullOrWhiteSpace(param.ConditionGroup) ? param.ParamName : param.ConditionGroup;
            if (enabledSet != null && !enabledSet.Contains(conditionKey))
            {
                continue; // 跳过禁用参数的验证
            }

            if (!request.Parameters.TryGetValue(param.ParamName, out var value) ||
                value == null || (value is string s && string.IsNullOrEmpty(s)))
            {
                return new QueryResult { Success = false, ErrorMessage = $"缺少必填参数: {param.ParamLabel}" };
            }
        }

        try
        {
            // 调试日志：记录参数信息
            _logger.LogInformation("Config query {Id} has {ParamCount} parameters defined",
                id, configQuery.Parameters.Count);
            foreach (var p in configQuery.Parameters)
            {
                _logger.LogInformation("Parameter: {Name}, Type: {Type}", p.ParamName, p.ParamType);
            }
            _logger.LogInformation("Request parameters: {Params}",
                string.Join(", ", request.Parameters.Select(kv => $"{kv.Key}={kv.Value}")));

            // 先移除禁用的条件行
            var sqlAfterConditionRemoval = RemoveDisabledConditions(
                configQuery.SqlContent,
                configQuery.Parameters.ToList(),
                request.EnabledConditions);

            _logger.LogInformation("SQL after condition removal: {Sql}", sqlAfterConditionRemoval);

            // 然后替换参数
            var sql = ReplaceParameters(sqlAfterConditionRemoval, configQuery.Parameters.ToList(), request.Parameters);

            _logger.LogInformation("SQL after parameter replacement: {Sql}", sql);

            var connectionString = EnsureSslSettings(_aesEncryptor.Decrypt(connection.ConnectionString));
            var timeoutSeconds = _configuration.GetValue<int>("Query:TimeoutSeconds", 30);
            var maxRows = _configuration.GetValue<int>("Query:MaxRows", 10000);

            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();

            using var command = new SqlCommand(sql, sqlConnection)
            {
                CommandTimeout = timeoutSeconds
            };

            using var reader = await command.ExecuteReaderAsync();

            var columns = new List<string>();
            var rows = new List<Dictionary<string, object?>>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            int rowCount = 0;
            while (await reader.ReadAsync() && rowCount < maxRows)
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                rows.Add(row);
                rowCount++;
            }

            stopwatch.Stop();

            // 记录日志
            await LogQueryAsync(userId, connection.PlatformCode, connection.Name, sql,
                (int)stopwatch.ElapsedMilliseconds, rowCount, QueryStatus.Success, null, clientIp);

            _logger.LogInformation("Config query {Id} executed successfully. Rows: {RowCount}, Time: {Time}ms",
                id, rowCount, stopwatch.ElapsedMilliseconds);

            return new QueryResult
            {
                Success = true,
                Columns = columns,
                Rows = rows,
                TotalRows = rowCount,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (SqlException ex)
        {
            stopwatch.Stop();
            var errorMessage = $"SQL执行错误: {ex.Message}";

            await LogQueryAsync(userId, connection.PlatformCode, connection.Name, configQuery.SqlContent,
                (int)stopwatch.ElapsedMilliseconds, 0, QueryStatus.Failed, errorMessage, clientIp);

            _logger.LogError(ex, "Config query {Id} execution failed", id);

            return new QueryResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMessage = $"查询执行失败: {ex.Message}";

            _logger.LogError(ex, "Config query {Id} execution failed", id);

            return new QueryResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public Task<ParseParamsResponse> ParseParamsAsync(string sql)
    {
        var parameters = new List<string>();
        var regex = new Regex(@"@(\w+)", RegexOptions.Compiled);
        var matches = regex.Matches(sql);

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            if (!parameters.Contains(paramName))
            {
                parameters.Add(paramName);
            }
        }

        return Task.FromResult(new ParseParamsResponse { Parameters = parameters });
    }

    public async Task<GetOptionsResponse> GetOptionsAsync(int userId, bool isAdmin, GetOptionsRequest request)
    {
        var connection = await _context.DatabaseConnections.FindAsync(request.ConnectionId);
        if (connection == null)
        {
            throw new InvalidOperationException("数据库连接不存在");
        }

        try
        {
            var connectionString = EnsureSslSettings(_aesEncryptor.Decrypt(connection.ConnectionString));
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();

            using var command = new SqlCommand(request.Sql, sqlConnection)
            {
                CommandTimeout = 30
            };

            using var reader = await command.ExecuteReaderAsync();

            var options = new List<OptionItem>();
            while (await reader.ReadAsync())
            {
                var value = reader.GetValue(0)?.ToString() ?? "";
                var label = reader.FieldCount > 1 ? reader.GetValue(1)?.ToString() ?? value : value;
                options.Add(new OptionItem { Value = value, Label = label });
            }

            return new GetOptionsResponse { Options = options };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get options for connection {ConnectionId}", request.ConnectionId);
            throw new InvalidOperationException($"获取选项失败: {ex.Message}");
        }
    }

    #endregion

    #region 导入导出

    public async Task<int> ImportAsync(int userId, bool isAdmin, string json)
    {
        var importData = JsonSerializer.Deserialize<ConfigQueryExportDto>(json, JsonOptions)
            ?? throw new InvalidOperationException("JSON 解析失败");

        var request = new CreateConfigQueryRequest
        {
            Name = importData.Name,
            Description = importData.Description,
            SqlContent = importData.Sql,
            ConnectionId = importData.ConnectionId,
            IsPublic = false,
            Parameters = importData.Parameters.Select(p => new CreateConfigQueryParameterRequest
            {
                ParamName = p.Name,
                ParamLabel = p.Label,
                ParamType = p.Type,
                IsRequired = p.Required,
                DefaultValue = p.DefaultValue,
                Placeholder = p.Placeholder,
                OptionsConfig = p.Options,
                ValidationRule = p.ValidationRule,
                ExtraConfig = p.ExtraConfig
            }).ToList()
        };

        return await CreateAsync(userId, isAdmin, request);
    }

    public async Task<ConfigQueryExportDto?> ExportAsync(int id, int userId, bool isAdmin)
    {
        var configQuery = await _context.ConfigQueries
            .Include(q => q.Parameters.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

        if (configQuery == null) return null;

        // 检查权限
        if (configQuery.CreatedBy != userId && !configQuery.IsPublic)
        {
            throw new UnauthorizedAccessException("无权导出此配置查询");
        }

        return new ConfigQueryExportDto
        {
            Name = configQuery.Name,
            Description = configQuery.Description,
            Sql = configQuery.SqlContent,
            ConnectionId = configQuery.ConnectionId,
            Parameters = configQuery.Parameters.Select(p => new ConfigQueryParameterExportDto
            {
                Name = p.ParamName,
                Label = p.ParamLabel,
                Type = p.ParamType,
                Required = p.IsRequired,
                DefaultValue = p.DefaultValue,
                Placeholder = p.Placeholder,
                Options = !string.IsNullOrEmpty(p.OptionsConfig)
                    ? JsonSerializer.Deserialize<OptionsConfigDto>(p.OptionsConfig, JsonOptions)
                    : null,
                ValidationRule = p.ValidationRule,
                ExtraConfig = !string.IsNullOrEmpty(p.ExtraConfig)
                    ? JsonSerializer.Deserialize<ExtraConfigDto>(p.ExtraConfig, JsonOptions)
                    : null
            }).ToList()
        };
    }

    #endregion

    #region 参数预设

    public async Task<List<ConfigQueryParamPresetDto>> GetPresetsAsync(int configQueryId, int userId)
    {
        var presets = await _context.ConfigQueryParamPresets
            .Where(p => p.ConfigQueryId == configQueryId && p.CreatedBy == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return presets.Select(p => new ConfigQueryParamPresetDto
        {
            Id = p.Id,
            Name = p.Name,
            ParamValues = !string.IsNullOrEmpty(p.ParamValues)
                ? JsonSerializer.Deserialize<Dictionary<string, object?>>(p.ParamValues, JsonOptions) ?? new()
                : new(),
            IsDefault = p.IsDefault,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<int> CreatePresetAsync(int configQueryId, int userId, CreateParamPresetRequest request)
    {
        // 如果设为默认，取消其他默认
        if (request.IsDefault)
        {
            var existingDefaults = await _context.ConfigQueryParamPresets
                .Where(p => p.ConfigQueryId == configQueryId && p.CreatedBy == userId && p.IsDefault)
                .ToListAsync();
            foreach (var preset in existingDefaults)
            {
                preset.IsDefault = false;
            }
        }

        var newPreset = new ConfigQueryParamPreset
        {
            ConfigQueryId = configQueryId,
            Name = request.Name,
            ParamValues = JsonSerializer.Serialize(request.ParamValues, JsonOptions),
            CreatedBy = userId,
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfigQueryParamPresets.Add(newPreset);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created preset {Id} for config query {ConfigQueryId} by user {UserId}",
            newPreset.Id, configQueryId, userId);
        return newPreset.Id;
    }

    public async Task<bool> UpdatePresetAsync(int configQueryId, int presetId, int userId, UpdateParamPresetRequest request)
    {
        var preset = await _context.ConfigQueryParamPresets
            .FirstOrDefaultAsync(p => p.Id == presetId && p.ConfigQueryId == configQueryId && p.CreatedBy == userId);

        if (preset == null) return false;

        if (!string.IsNullOrEmpty(request.Name)) preset.Name = request.Name;
        if (request.ParamValues != null) preset.ParamValues = JsonSerializer.Serialize(request.ParamValues, JsonOptions);

        if (request.IsDefault.HasValue)
        {
            if (request.IsDefault.Value)
            {
                // 取消其他默认
                var existingDefaults = await _context.ConfigQueryParamPresets
                    .Where(p => p.ConfigQueryId == configQueryId && p.CreatedBy == userId && p.IsDefault && p.Id != presetId)
                    .ToListAsync();
                foreach (var p in existingDefaults)
                {
                    p.IsDefault = false;
                }
            }
            preset.IsDefault = request.IsDefault.Value;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated preset {Id}", presetId);
        return true;
    }

    public async Task<bool> DeletePresetAsync(int configQueryId, int presetId, int userId)
    {
        var preset = await _context.ConfigQueryParamPresets
            .FirstOrDefaultAsync(p => p.Id == presetId && p.ConfigQueryId == configQueryId && p.CreatedBy == userId);

        if (preset == null) return false;

        _context.ConfigQueryParamPresets.Remove(preset);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted preset {Id}", presetId);
        return true;
    }

    #endregion

    #region 私有方法

    private ConfigQueryDetailDto MapToDetailDto(ConfigQuery query, int userId, bool isAdmin)
    {
        return new ConfigQueryDetailDto
        {
            Id = query.Id,
            Name = query.Name,
            Description = query.Description,
            SqlContent = query.SqlContent,
            ConnectionId = query.ConnectionId,
            ConnectionName = query.Connection?.Name,
            IsPublic = query.IsPublic,
            CreatedBy = query.CreatedBy,
            CreatedByName = query.Creator?.Nickname ?? "",
            IsOwner = query.CreatedBy == userId,
            CanEdit = query.CreatedBy == userId || isAdmin,
            SortOrder = query.SortOrder,
            CreatedAt = query.CreatedAt,
            UpdatedAt = query.UpdatedAt,
            Parameters = query.Parameters.Select(p => new ConfigQueryParameterDto
            {
                Id = p.Id,
                ParamName = p.ParamName,
                ParamLabel = p.ParamLabel,
                ParamType = p.ParamType,
                IsRequired = p.IsRequired,
                DefaultValue = p.DefaultValue,
                Placeholder = p.Placeholder,
                OptionsConfig = !string.IsNullOrEmpty(p.OptionsConfig)
                    ? JsonSerializer.Deserialize<OptionsConfigDto>(p.OptionsConfig, JsonOptions)
                    : null,
                ValidationRule = p.ValidationRule,
                ExtraConfig = !string.IsNullOrEmpty(p.ExtraConfig)
                    ? JsonSerializer.Deserialize<ExtraConfigDto>(p.ExtraConfig, JsonOptions)
                    : null,
                SortOrder = p.SortOrder,
                ConditionGroup = p.ConditionGroup
            }).ToList()
        };
    }

    private string ReplaceParameters(string sql, List<ConfigQueryParameter> parameters, Dictionary<string, object?> values)
    {
        var result = sql;

        foreach (var param in parameters)
        {
            // Trim 参数名，防止数据库中存储的参数名有空格
            var trimmedParamName = param.ParamName.Trim();

            // 尝试从 values 中获取值（先尝试原始参数名，再尝试 trim 后的参数名）
            if (!values.TryGetValue(param.ParamName, out var value) &&
                !values.TryGetValue(trimmedParamName, out value))
            {
                value = param.DefaultValue;
            }

            var placeholder = $"@{trimmedParamName}";
            var replacement = FormatParameterValue(param.ParamType, value);

            result = result.Replace(placeholder, replacement);
        }

        return result;
    }

    private string FormatParameterValue(string paramType, object? value)
    {
        if (value == null) return "NULL";

        return paramType.ToLower() switch
        {
            "number" => FormatNumber(value),
            "text" => EscapeString(value.ToString() ?? ""),
            "date" => EscapeString(value.ToString() ?? ""),
            "daterange" => FormatDateRange(value),
            "select" => EscapeString(value.ToString() ?? ""),
            "multiselect" => FormatMultiSelect(value),
            _ => EscapeString(value.ToString() ?? "")
        };
    }

    private string FormatNumber(object value)
    {
        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number)
            {
                return je.GetDecimal().ToString();
            }
            return je.GetString() ?? "0";
        }
        return value.ToString() ?? "0";
    }

    private string EscapeString(string value)
    {
        // 防止 SQL 注入
        var escaped = value.Replace("'", "''");
        // 禁止注释符号
        escaped = escaped.Replace("--", "").Replace("/*", "").Replace("*/", "");
        return $"'{escaped}'";
    }

    private string FormatDateRange(object value)
    {
        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var dates = new List<string>();
            foreach (var item in je.EnumerateArray())
            {
                dates.Add(item.GetString() ?? "");
            }
            if (dates.Count >= 2)
            {
                return $"'{dates[0]}' AND '{dates[1]}'";
            }
        }
        return "NULL AND NULL";
    }

    private string FormatMultiSelect(object value)
    {
        var items = new List<string>();

        if (value is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
            {
                var str = item.GetString() ?? "";
                items.Add($"'{str.Replace("'", "''")}'");
            }
        }
        else if (value is IEnumerable<string> strList)
        {
            foreach (var item in strList)
            {
                items.Add($"'{item.Replace("'", "''")}'");
            }
        }

        return items.Count > 0 ? string.Join(",", items) : "''";
    }

    /// <summary>
    /// 根据启用状态移除SQL中的条件行
    /// </summary>
    private string RemoveDisabledConditions(
        string sql,
        List<ConfigQueryParameter> parameters,
        List<string>? enabledConditions)
    {
        // null表示全部启用
        if (enabledConditions == null) return sql;

        // 构建需要保留的条件组/参数名集合
        var enabledSet = new HashSet<string>(enabledConditions, StringComparer.OrdinalIgnoreCase);

        // 构建禁用的参数名集合
        var disabledParams = new List<string>();
        foreach (var param in parameters)
        {
            var conditionKey = string.IsNullOrWhiteSpace(param.ConditionGroup) ? param.ParamName : param.ConditionGroup;
            var isEnabled = enabledSet.Contains(conditionKey);
            _logger.LogInformation("Param: '{ParamName}', ConditionGroup: '{ConditionGroup}', ConditionKey: '{ConditionKey}', IsEnabled: {IsEnabled}",
                param.ParamName, param.ConditionGroup ?? "(null)", conditionKey, isEnabled);
            if (!isEnabled)
            {
                disabledParams.Add(param.ParamName);
            }
        }

        if (disabledParams.Count == 0) return sql;

        _logger.LogInformation("Disabled params to remove: {Params}", string.Join(", ", disabledParams));

        var result = sql;

        // 移除包含禁用参数的 AND/OR 条件
        foreach (var paramName in disabledParams)
        {
            // Trim 参数名，防止数据库中存储的参数名有空格
            var trimmedParamName = paramName.Trim();

            // 匹配模式: and/or 字段名(支持 a.[中文] 格式) 运算符 @参数名
            // 字段名可以是: a.[xxx], [xxx], a.xxx, xxx 等格式
            var pattern = $@"\s+(AND|OR)\s+[a-zA-Z0-9_\.]*\[?[^\]]*\]?\s*=\s*@{Regex.Escape(trimmedParamName)}\b";
            result = Regex.Replace(result, pattern, " ", RegexOptions.IgnoreCase);

            // 也处理 LIKE 情况
            pattern = $@"\s+(AND|OR)\s+[a-zA-Z0-9_\.]*\[?[^\]]*\]?\s+LIKE\s+[^@]*@{Regex.Escape(trimmedParamName)}\b[^']*";
            result = Regex.Replace(result, pattern, " ", RegexOptions.IgnoreCase);

            // 处理 IN 情况
            pattern = $@"\s+(AND|OR)\s+[a-zA-Z0-9_\.]*\[?[^\]]*\]?\s+IN\s*\([^)]*@{Regex.Escape(trimmedParamName)}\b[^)]*\)";
            result = Regex.Replace(result, pattern, " ", RegexOptions.IgnoreCase);
        }

        // 清理多余的空白
        result = Regex.Replace(result, @"[ \t]+", " ");
        result = Regex.Replace(result, @"\n\s*\n", "\n");

        // 清理多余的AND/OR
        result = CleanupWhereClause(result);

        _logger.LogInformation("SQL after removing disabled conditions:\n{Sql}", result);

        return result;
    }

    /// <summary>
    /// 清理WHERE子句中悬空的AND/OR
    /// </summary>
    private string CleanupWhereClause(string sql)
    {
        // 移除WHERE后直接跟AND/OR的情况（考虑换行和空格）
        sql = Regex.Replace(sql, @"WHERE\s+(AND|OR)\s+", "WHERE ", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 移除1=1后直接跟AND的多余空行情况
        sql = Regex.Replace(sql, @"1\s*=\s*1\s+(AND|OR)\s+", "1=1 $1 ", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 移除连续的AND/OR
        sql = Regex.Replace(sql, @"\s+(AND|OR)\s+(AND|OR)\s+", " $2 ", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 移除末尾悬空的AND/OR (在GROUP BY/ORDER BY/LIMIT/HAVING等之前)
        sql = Regex.Replace(sql, @"\s+(AND|OR)\s*(?=\s*(GROUP|ORDER|LIMIT|HAVING|;|$))", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 移除空的WHERE 1=1子句后面没有条件的情况 (WHERE 1=1 直接跟 GROUP/ORDER等)
        sql = Regex.Replace(sql, @"WHERE\s+1\s*=\s*1\s*(?=\s*(GROUP|ORDER|LIMIT|HAVING|;|$))", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 移除完全空的WHERE子句
        sql = Regex.Replace(sql, @"WHERE\s+(?=(GROUP|ORDER|LIMIT|HAVING|;|$))", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return sql.Trim();
    }

    private async Task LogQueryAsync(
        int userId,
        string? platformCode,
        string? databaseName,
        string sql,
        int executionTimeMs,
        int rowCount,
        QueryStatus status,
        string? errorMessage,
        string? clientIp)
    {
        var log = new QueryLog
        {
            UserId = userId,
            PlatformCode = platformCode,
            DatabaseName = databaseName,
            SqlContent = sql,
            ExecutionTimeMs = executionTimeMs,
            RowCount = rowCount,
            Status = status,
            ErrorMessage = errorMessage,
            ClientIp = clientIp,
            CreatedAt = DateTime.UtcNow
        };

        _context.QueryLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region 文件夹管理

    public async Task<List<ConfigQueryFolderDto>> GetFoldersAsync(int userId)
    {
        var folders = await _context.ConfigQueryFolders
            .Where(f => f.CreatedBy == userId)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .Select(f => new ConfigQueryFolderDto
            {
                Id = f.Id,
                Name = f.Name,
                SortOrder = f.SortOrder,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return folders;
    }

    public async Task<int> CreateFolderAsync(int userId, CreateConfigQueryFolderRequest request)
    {
        var folder = new ConfigQueryFolder
        {
            Name = request.Name,
            CreatedBy = userId,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfigQueryFolders.Add(folder);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created config query folder {Id} by user {UserId}", folder.Id, userId);
        return folder.Id;
    }

    public async Task<bool> UpdateFolderAsync(int folderId, int userId, UpdateConfigQueryFolderRequest request)
    {
        var folder = await _context.ConfigQueryFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.CreatedBy == userId);

        if (folder == null) return false;

        if (!string.IsNullOrEmpty(request.Name)) folder.Name = request.Name;
        if (request.SortOrder.HasValue) folder.SortOrder = request.SortOrder.Value;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated config query folder {Id}", folderId);
        return true;
    }

    public async Task<bool> DeleteFolderAsync(int folderId, int userId)
    {
        var folder = await _context.ConfigQueryFolders
            .FirstOrDefaultAsync(f => f.Id == folderId && f.CreatedBy == userId);

        if (folder == null) return false;

        // 将文件夹内的查询移到未分组
        var queries = await _context.ConfigQueries
            .Where(q => q.FolderId == folderId)
            .ToListAsync();
        foreach (var q in queries)
        {
            q.FolderId = null;
        }

        _context.ConfigQueryFolders.Remove(folder);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted config query folder {Id}", folderId);
        return true;
    }

    public async Task<bool> MoveToFolderAsync(int configQueryId, int userId, int? folderId)
    {
        var configQuery = await _context.ConfigQueries
            .FirstOrDefaultAsync(q => q.Id == configQueryId && q.IsActive && q.CreatedBy == userId);

        if (configQuery == null) return false;

        // 如果指定了文件夹，验证文件夹存在且属于该用户
        if (folderId.HasValue)
        {
            var folderExists = await _context.ConfigQueryFolders
                .AnyAsync(f => f.Id == folderId.Value && f.CreatedBy == userId);
            if (!folderExists) return false;
        }

        configQuery.FolderId = folderId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Moved config query {Id} to folder {FolderId}", configQueryId, folderId);
        return true;
    }

    #endregion

    /// <summary>
    /// 确保连接字符串包含SSL相关设置，避免证书验证错误
    /// </summary>
    private static string EnsureSslSettings(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        // 如果连接字符串中已经包含 TrustServerCertificate 或 Encrypt 设置，则不做修改
        if (connectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        // 添加 TrustServerCertificate=True 以信任服务器证书
        var separator = connectionString.TrimEnd().EndsWith(';') ? "" : ";";
        return connectionString + separator + "TrustServerCertificate=True";
    }
}
