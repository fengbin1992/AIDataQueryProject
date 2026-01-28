using AIDataQuery.API.Models.DTOs.Query;

namespace AIDataQuery.API.Services.Interfaces;

public interface IQueryService
{
    Task<QueryResult> ExecuteQueryAsync(int userId, QueryRequest request, string? clientIp);
    Task<List<TableInfo>> GetTablesAsync(int connectionId);
    Task<List<ColumnInfo>> GetColumnsAsync(int connectionId, string tableName);
    Task<bool> TestConnectionAsync(int connectionId);
    Task<byte[]> ExportToCsvAsync(QueryRequest request);
    Task<byte[]> ExportToExcelAsync(QueryRequest request);
}
