using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.QueryTab;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Services;

/// <summary>
/// 查询标签页服务实现
/// </summary>
public class QueryTabService : IQueryTabService
{
    private readonly AppDbContext _context;
    private readonly ILogger<QueryTabService> _logger;

    public QueryTabService(AppDbContext context, ILogger<QueryTabService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<QueryTabDto>> GetUserTabsAsync(int userId)
    {
        var tabs = await _context.QueryTabs
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.SortOrder)
            .Select(t => new QueryTabDto
            {
                Id = t.Id,
                Name = t.Name,
                PlatformCode = t.PlatformCode,
                ConnectionId = t.ConnectionId,
                SqlContent = t.SqlContent,
                SortOrder = t.SortOrder,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return tabs;
    }

    public async Task<QueryTabDto?> GetTabByIdAsync(int id, int userId)
    {
        var tab = await _context.QueryTabs
            .Where(t => t.Id == id && t.UserId == userId)
            .Select(t => new QueryTabDto
            {
                Id = t.Id,
                Name = t.Name,
                PlatformCode = t.PlatformCode,
                ConnectionId = t.ConnectionId,
                SqlContent = t.SqlContent,
                SortOrder = t.SortOrder,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return tab;
    }

    public async Task<QueryTabDto> CreateTabAsync(int userId, CreateQueryTabRequest request)
    {
        // 获取当前用户最大排序号
        var maxSortOrder = await _context.QueryTabs
            .Where(t => t.UserId == userId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        var tab = new QueryTab
        {
            UserId = userId,
            Name = request.Name,
            PlatformCode = request.PlatformCode,
            ConnectionId = request.ConnectionId,
            SqlContent = request.SqlContent,
            SortOrder = maxSortOrder + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.QueryTabs.Add(tab);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created query tab {TabId}: {TabName}", userId, tab.Id, tab.Name);

        return new QueryTabDto
        {
            Id = tab.Id,
            Name = tab.Name,
            PlatformCode = tab.PlatformCode,
            ConnectionId = tab.ConnectionId,
            SqlContent = tab.SqlContent,
            SortOrder = tab.SortOrder,
            CreatedAt = tab.CreatedAt,
            UpdatedAt = tab.UpdatedAt
        };
    }

    public async Task<QueryTabDto?> UpdateTabAsync(int id, int userId, UpdateQueryTabRequest request)
    {
        var tab = await _context.QueryTabs
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tab == null) return null;

        if (!string.IsNullOrEmpty(request.Name)) tab.Name = request.Name;
        if (request.PlatformCode != null) tab.PlatformCode = request.PlatformCode;
        if (request.ConnectionId.HasValue) tab.ConnectionId = request.ConnectionId;
        if (request.SqlContent != null) tab.SqlContent = request.SqlContent;
        tab.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated query tab {TabId}", userId, id);

        return new QueryTabDto
        {
            Id = tab.Id,
            Name = tab.Name,
            PlatformCode = tab.PlatformCode,
            ConnectionId = tab.ConnectionId,
            SqlContent = tab.SqlContent,
            SortOrder = tab.SortOrder,
            CreatedAt = tab.CreatedAt,
            UpdatedAt = tab.UpdatedAt
        };
    }

    public async Task<bool> DeleteTabAsync(int id, int userId)
    {
        var tab = await _context.QueryTabs
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (tab == null) return false;

        _context.QueryTabs.Remove(tab);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted query tab {TabId}", userId, id);

        return true;
    }

    public async Task<bool> ReorderTabsAsync(int userId, ReorderQueryTabsRequest request)
    {
        var tabs = await _context.QueryTabs
            .Where(t => t.UserId == userId && request.TabIds.Contains(t.Id))
            .ToListAsync();

        // 验证所有ID都属于当前用户
        if (tabs.Count != request.TabIds.Count)
        {
            _logger.LogWarning("User {UserId} attempted to reorder tabs with invalid IDs", userId);
            return false;
        }

        // 按请求的顺序更新排序号
        for (int i = 0; i < request.TabIds.Count; i++)
        {
            var tab = tabs.FirstOrDefault(t => t.Id == request.TabIds[i]);
            if (tab != null)
            {
                tab.SortOrder = i;
                tab.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} reordered {Count} query tabs", userId, tabs.Count);

        return true;
    }
}
