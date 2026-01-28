using AIDataQuery.API.Models.DTOs.QueryLog;
using AIDataQuery.API.Models.DTOs.Common;

namespace AIDataQuery.API.Services.Interfaces;

public interface IQueryLogService
{
    Task<PagedResult<QueryLogDto>> GetLogsAsync(int userId, bool isAdmin, QueryLogParams queryParams);
    Task<QueryLogDto?> GetLogByIdAsync(long id, int userId, bool isAdmin);
}
