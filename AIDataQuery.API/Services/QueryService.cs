using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.Query;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Models.Enums;
using AIDataQuery.API.Services.Interfaces;
using AIDataQuery.API.Infrastructure.Security;
using AIDataQuery.API.Infrastructure.Encryption;
using AIDataQuery.API.Infrastructure.Database;

namespace AIDataQuery.API.Services;

public class QueryService : IQueryService
{
    private readonly AppDbContext _context;
    private readonly ISqlValidator _sqlValidator;
    private readonly IAesEncryptor _aesEncryptor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QueryService> _logger;

    public QueryService(
        AppDbContext context,
        ISqlValidator sqlValidator,
        IAesEncryptor aesEncryptor,
        IConfiguration configuration,
        ILogger<QueryService> logger)
    {
        _context = context;
        _sqlValidator = sqlValidator;
        _aesEncryptor = aesEncryptor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<QueryResult> ExecuteQueryAsync(int userId, QueryRequest request, string? clientIp)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryResult();

        // Validate SQL
        var validationResult = _sqlValidator.Validate(request.Sql);
        if (!validationResult.IsValid)
        {
            await LogQueryAsync(userId, request, 0, 0, QueryStatus.Failed, validationResult.ErrorMessage, clientIp);
            return new QueryResult
            {
                Success = false,
                ErrorMessage = validationResult.ErrorMessage
            };
        }

        try
        {
            var connection = await _context.DatabaseConnections.FindAsync(request.ConnectionId);
            if (connection == null)
            {
                return new QueryResult
                {
                    Success = false,
                    ErrorMessage = "数据库连接不存在"
                };
            }

            var connectionString = _aesEncryptor.Decrypt(connection.ConnectionString);
            var timeoutSeconds = _configuration.GetValue<int>("Query:TimeoutSeconds", 30);
            var maxRows = _configuration.GetValue<int>("Query:MaxRows", 10000);

            using var dbConnection = DbConnectionFactory.CreateConnection(connection.DatabaseType, connectionString);
            await dbConnection.OpenAsync();

            using var command = DbConnectionFactory.CreateCommand(request.Sql, dbConnection);
            command.CommandTimeout = timeoutSeconds;

            using var reader = await command.ExecuteReaderAsync();

            // Get columns
            for (int i = 0; i < reader.FieldCount; i++)
            {
                result.Columns.Add(reader.GetName(i));
            }

            // Get rows
            int rowCount = 0;
            while (await reader.ReadAsync() && rowCount < maxRows)
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                result.Rows.Add(row);
                rowCount++;
            }

            result.TotalRows = rowCount;
            result.Success = true;

            stopwatch.Stop();
            result.ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds;

            await LogQueryAsync(userId, request, result.ExecutionTimeMs, rowCount, QueryStatus.Success, null, clientIp, connection.Name);

            _logger.LogInformation("Query executed successfully. Rows: {RowCount}, Time: {Time}ms",
                rowCount, result.ExecutionTimeMs);

            return result;
        }
        catch (DbException ex)
        {
            stopwatch.Stop();
            var errorMessage = $"SQL执行错误: {ex.Message}";
            await LogQueryAsync(userId, request, (int)stopwatch.ElapsedMilliseconds, 0, QueryStatus.Failed, errorMessage, clientIp);

            _logger.LogError(ex, "SQL execution failed for user {UserId}", userId);

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
            await LogQueryAsync(userId, request, (int)stopwatch.ElapsedMilliseconds, 0, QueryStatus.Failed, errorMessage, clientIp);

            _logger.LogError(ex, "Query execution failed for user {UserId}", userId);

            return new QueryResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<List<TableInfo>> GetTablesAsync(int connectionId)
    {
        var connection = await _context.DatabaseConnections.FindAsync(connectionId);
        if (connection == null) return new List<TableInfo>();

        try
        {
            var connectionString = _aesEncryptor.Decrypt(connection.ConnectionString);
            using var dbConnection = DbConnectionFactory.CreateConnection(connection.DatabaseType, connectionString);
            await dbConnection.OpenAsync();

            var sql = DbConnectionFactory.GetTablesSql(connection.DatabaseType);

            using var command = DbConnectionFactory.CreateCommand(sql, dbConnection);
            using var reader = await command.ExecuteReaderAsync();

            var tables = new List<TableInfo>();
            while (await reader.ReadAsync())
            {
                tables.Add(new TableInfo
                {
                    Schema = reader.GetString(0),
                    Name = reader.GetString(1)
                });
            }

            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tables for connection {ConnectionId}", connectionId);
            return new List<TableInfo>();
        }
    }

    public async Task<List<ColumnInfo>> GetColumnsAsync(int connectionId, string tableName)
    {
        var connection = await _context.DatabaseConnections.FindAsync(connectionId);
        if (connection == null) return new List<ColumnInfo>();

        try
        {
            var connectionString = _aesEncryptor.Decrypt(connection.ConnectionString);
            using var dbConnection = DbConnectionFactory.CreateConnection(connection.DatabaseType, connectionString);
            await dbConnection.OpenAsync();

            var sql = DbConnectionFactory.GetColumnsSql(connection.DatabaseType);

            using var command = DbConnectionFactory.CreateCommand(sql, dbConnection);
            var param = command.CreateParameter();
            param.ParameterName = "@TableName";
            param.Value = tableName;
            command.Parameters.Add(param);

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

    public async Task<bool> TestConnectionAsync(int connectionId)
    {
        var connection = await _context.DatabaseConnections.FindAsync(connectionId);
        if (connection == null) return false;

        try
        {
            var connectionString = _aesEncryptor.Decrypt(connection.ConnectionString);
            using var dbConnection = DbConnectionFactory.CreateConnection(connection.DatabaseType, connectionString);
            await dbConnection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed for connection {ConnectionId}", connectionId);
            return false;
        }
    }

    public async Task<byte[]> ExportToCsvAsync(QueryRequest request)
    {
        var result = await ExecuteQueryWithoutLogging(request);
        if (!result.Success || result.Columns.Count == 0)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "查询失败");
        }

        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", result.Columns.Select(c => $"\"{c}\"")));

        // Rows
        foreach (var row in result.Rows)
        {
            var values = result.Columns.Select(col =>
            {
                var val = row.GetValueOrDefault(col);
                if (val == null) return "";
                var str = val.ToString() ?? "";
                return $"\"{str.Replace("\"", "\"\"")}\"";
            });
            sb.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportToExcelAsync(QueryRequest request)
    {
        var result = await ExecuteQueryWithoutLogging(request);
        if (!result.Success || result.Columns.Count == 0)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "查询失败");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("查询结果");

        // Header
        for (int i = 0; i < result.Columns.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = result.Columns[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        // Rows
        for (int rowIndex = 0; rowIndex < result.Rows.Count; rowIndex++)
        {
            var row = result.Rows[rowIndex];
            for (int colIndex = 0; colIndex < result.Columns.Count; colIndex++)
            {
                var val = row.GetValueOrDefault(result.Columns[colIndex]);
                if (val != null)
                {
                    worksheet.Cell(rowIndex + 2, colIndex + 1).Value = val.ToString();
                }
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task<QueryResult> ExecuteQueryWithoutLogging(QueryRequest request)
    {
        var validationResult = _sqlValidator.Validate(request.Sql);
        if (!validationResult.IsValid)
        {
            return new QueryResult { Success = false, ErrorMessage = validationResult.ErrorMessage };
        }

        var connection = await _context.DatabaseConnections.FindAsync(request.ConnectionId);
        if (connection == null)
        {
            return new QueryResult { Success = false, ErrorMessage = "数据库连接不存在" };
        }

        var maxExportRows = _configuration.GetValue<int>("Query:MaxExportRows", 50000);
        var connectionString = _aesEncryptor.Decrypt(connection.ConnectionString);

        using var dbConnection = DbConnectionFactory.CreateConnection(connection.DatabaseType, connectionString);
        await dbConnection.OpenAsync();

        using var command = DbConnectionFactory.CreateCommand(request.Sql, dbConnection);
        using var reader = await command.ExecuteReaderAsync();

        var result = new QueryResult { Success = true };

        for (int i = 0; i < reader.FieldCount; i++)
        {
            result.Columns.Add(reader.GetName(i));
        }

        int rowCount = 0;
        while (await reader.ReadAsync() && rowCount < maxExportRows)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row[reader.GetName(i)] = value == DBNull.Value ? null : value;
            }
            result.Rows.Add(row);
            rowCount++;
        }

        result.TotalRows = rowCount;
        return result;
    }

    private async Task LogQueryAsync(
        int userId,
        QueryRequest request,
        int executionTimeMs,
        int rowCount,
        QueryStatus status,
        string? errorMessage,
        string? clientIp,
        string? databaseName = null)
    {
        var log = new QueryLog
        {
            UserId = userId,
            PlatformCode = request.PlatformCode,
            DatabaseName = databaseName,
            SqlContent = request.Sql,
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

}
