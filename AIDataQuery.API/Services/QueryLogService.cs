using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.QueryLog;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Services;

public class QueryLogService : IQueryLogService
{
    private readonly AppDbContext _context;

    public QueryLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<QueryLogDto>> GetLogsAsync(int userId, bool isAdmin, QueryLogParams queryParams)
    {
        var query = _context.QueryLogs
            .Include(l => l.User)
            .AsQueryable();

        // Non-admin can only see their own logs
        if (!isAdmin)
        {
            query = query.Where(l => l.UserId == userId);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(queryParams.PlatformCode))
        {
            query = query.Where(l => l.PlatformCode == queryParams.PlatformCode);
        }

        if (queryParams.Status.HasValue)
        {
            query = query.Where(l => l.Status == queryParams.Status.Value);
        }

        if (queryParams.StartDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= queryParams.StartDate.Value);
        }

        if (queryParams.EndDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= queryParams.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Keyword))
        {
            query = query.Where(l => l.SqlContent.Contains(queryParams.Keyword));
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((queryParams.PageIndex - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedResult<QueryLogDto>
        {
            Items = logs.Select(l => new QueryLogDto
            {
                Id = l.Id,
                Username = l.User?.Username ?? "",
                PlatformCode = l.PlatformCode,
                DatabaseName = l.DatabaseName,
                SqlContent = l.SqlContent,
                ExecutionTimeMs = l.ExecutionTimeMs,
                RowCount = l.RowCount,
                Status = l.Status,
                ErrorMessage = l.ErrorMessage,
                ClientIp = l.ClientIp,
                CreatedAt = l.CreatedAt
            }).ToList(),
            TotalCount = totalCount,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<QueryLogDto?> GetLogByIdAsync(long id, int userId, bool isAdmin)
    {
        var query = _context.QueryLogs
            .Include(l => l.User)
            .Where(l => l.Id == id);

        if (!isAdmin)
        {
            query = query.Where(l => l.UserId == userId);
        }

        var log = await query.FirstOrDefaultAsync();
        if (log == null) return null;

        return new QueryLogDto
        {
            Id = log.Id,
            Username = log.User?.Username ?? "",
            PlatformCode = log.PlatformCode,
            DatabaseName = log.DatabaseName,
            SqlContent = log.SqlContent,
            ExecutionTimeMs = log.ExecutionTimeMs,
            RowCount = log.RowCount,
            Status = log.Status,
            ErrorMessage = log.ErrorMessage,
            ClientIp = log.ClientIp,
            CreatedAt = log.CreatedAt
        };
    }
}
