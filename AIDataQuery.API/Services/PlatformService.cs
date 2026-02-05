using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.Platform;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Models.Enums;
using AIDataQuery.API.Services.Interfaces;
using AIDataQuery.API.Infrastructure.Encryption;

namespace AIDataQuery.API.Services;

public class PlatformService : IPlatformService
{
    private readonly AppDbContext _context;
    private readonly IAesEncryptor _aesEncryptor;
    private readonly ILogger<PlatformService> _logger;

    public PlatformService(
        AppDbContext context,
        IAesEncryptor aesEncryptor,
        ILogger<PlatformService> logger)
    {
        _context = context;
        _aesEncryptor = aesEncryptor;
        _logger = logger;
    }

    public async Task<List<PlatformDto>> GetPlatformsAsync(int userId)
    {
        // Get user info to check role
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return new List<PlatformDto>();
        }

        List<Platform> platforms;

        // Admin can see all active platforms without permission check
        if (user.Role == UserRole.Admin)
        {
            platforms = await _context.Platforms
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }
        else
        {
            // Get user's permitted platforms
            var permittedCodes = await _context.UserPlatformPermissions
                .Where(p => p.UserId == userId)
                .Select(p => p.PlatformCode)
                .ToListAsync();

            platforms = await _context.Platforms
                .Where(p => p.IsActive && permittedCodes.Contains(p.Code))
                .OrderBy(p => p.SortOrder)
                .ToListAsync();
        }

        var connectionCounts = await _context.DatabaseConnections
            .Where(c => c.IsActive)
            .GroupBy(c => c.PlatformCode)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, x => x.Count);

        return platforms.Select(p => new PlatformDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive,
            ConnectionCount = connectionCounts.GetValueOrDefault(p.Code, 0)
        }).ToList();
    }

    public async Task<List<PlatformDto>> GetAllPlatformsAsync()
    {
        var platforms = await _context.Platforms
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var connectionCounts = await _context.DatabaseConnections
            .GroupBy(c => c.PlatformCode)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Code, x => x.Count);

        return platforms.Select(p => new PlatformDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive,
            ConnectionCount = connectionCounts.GetValueOrDefault(p.Code, 0)
        }).ToList();
    }

    public async Task<PlatformDto?> GetPlatformByIdAsync(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null) return null;

        var connectionCount = await _context.DatabaseConnections
            .CountAsync(c => c.PlatformCode == platform.Code);

        return new PlatformDto
        {
            Id = platform.Id,
            Code = platform.Code,
            Name = platform.Name,
            Description = platform.Description,
            IsActive = platform.IsActive,
            ConnectionCount = connectionCount
        };
    }

    public async Task<PlatformDto?> CreatePlatformAsync(CreatePlatformRequest request)
    {
        // 检查编码是否已存在
        var exists = await _context.Platforms.AnyAsync(p => p.Code == request.Code);
        if (exists)
        {
            _logger.LogWarning("Platform with code {Code} already exists", request.Code);
            return null;
        }

        var platform = new Platform
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Platforms.Add(platform);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created platform {PlatformId} with code {Code}", platform.Id, platform.Code);

        return new PlatformDto
        {
            Id = platform.Id,
            Code = platform.Code,
            Name = platform.Name,
            Description = platform.Description,
            IsActive = platform.IsActive,
            ConnectionCount = 0
        };
    }

    public async Task<bool> UpdatePlatformAsync(int id, UpdatePlatformRequest request)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null) return false;

        platform.Name = request.Name;
        platform.Description = request.Description;
        platform.SortOrder = request.SortOrder;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated platform {PlatformId}", id);
        return true;
    }

    public async Task<bool> TogglePlatformStatusAsync(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null) return false;

        platform.IsActive = !platform.IsActive;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Toggled platform {PlatformId} status to {IsActive}", id, platform.IsActive);
        return true;
    }

    public async Task<bool> DeletePlatformAsync(int id)
    {
        var platform = await _context.Platforms.FindAsync(id);
        if (platform == null) return false;

        // 检查是否有关联的数据库连接
        var hasConnections = await _context.DatabaseConnections
            .AnyAsync(c => c.PlatformCode == platform.Code);

        if (hasConnections)
        {
            _logger.LogWarning("Cannot delete platform {PlatformId} because it has connections", id);
            return false;
        }

        // 删除用户平台权限
        var permissions = await _context.UserPlatformPermissions
            .Where(p => p.PlatformCode == platform.Code)
            .ToListAsync();
        _context.UserPlatformPermissions.RemoveRange(permissions);

        _context.Platforms.Remove(platform);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted platform {PlatformId}", id);
        return true;
    }

    public async Task<List<DatabaseConnectionDto>> GetConnectionsByPlatformAsync(string platformCode, int userId)
    {
        // Get user info to check role
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return new List<DatabaseConnectionDto>();
        }

        // Admin can see all connections without permission check
        if (user.Role == UserRole.Admin)
        {
            var allConnections = await _context.DatabaseConnections
                .Where(c => c.PlatformCode == platformCode && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            return allConnections.Select(c => new DatabaseConnectionDto
            {
                Id = c.Id,
                Name = c.Name,
                PlatformCode = c.PlatformCode,
                DatabaseType = c.DatabaseType,
                Description = c.Description,
                IsProduction = c.IsProduction,
                IsActive = c.IsActive
            }).ToList();
        }

        // For regular users, verify platform permission
        var hasPermission = await _context.UserPlatformPermissions
            .AnyAsync(p => p.UserId == userId && p.PlatformCode == platformCode);

        if (!hasPermission)
        {
            return new List<DatabaseConnectionDto>();
        }

        // Get user's permitted connection IDs
        var permittedConnectionIds = await _context.UserConnectionPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.ConnectionId)
            .ToListAsync();

        // Filter connections by platform and user's connection permissions
        var connections = await _context.DatabaseConnections
            .Where(c => c.PlatformCode == platformCode && c.IsActive && permittedConnectionIds.Contains(c.Id))
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return connections.Select(c => new DatabaseConnectionDto
        {
            Id = c.Id,
            Name = c.Name,
            PlatformCode = c.PlatformCode,
            DatabaseType = c.DatabaseType,
            Description = c.Description,
            IsProduction = c.IsProduction,
            IsActive = c.IsActive
            // 普通用户不返回连接字符串
        }).ToList();
    }

    public async Task<List<DatabaseConnectionDto>> GetAllConnectionsAsync()
    {
        // 管理员可以看到所有连接，包括被禁用的
        var connections = await _context.DatabaseConnections
            .OrderBy(c => c.PlatformCode)
            .ThenBy(c => c.SortOrder)
            .ToListAsync();

        return connections.Select(c => new DatabaseConnectionDto
        {
            Id = c.Id,
            Name = c.Name,
            PlatformCode = c.PlatformCode,
            DatabaseType = c.DatabaseType,
            Description = c.Description,
            IsProduction = c.IsProduction,
            IsActive = c.IsActive,
            ConnectionString = _aesEncryptor.Decrypt(c.ConnectionString)
        }).ToList();
    }

    public async Task<DatabaseConnectionDto?> GetConnectionByIdAsync(int id)
    {
        var connection = await _context.DatabaseConnections.FindAsync(id);
        if (connection == null) return null;

        return new DatabaseConnectionDto
        {
            Id = connection.Id,
            Name = connection.Name,
            PlatformCode = connection.PlatformCode,
            DatabaseType = connection.DatabaseType,
            Description = connection.Description,
            IsProduction = connection.IsProduction,
            IsActive = connection.IsActive,
            ConnectionString = _aesEncryptor.Decrypt(connection.ConnectionString)
        };
    }

    public async Task<DatabaseConnectionDto?> CreateConnectionAsync(CreateConnectionRequest request)
    {
        // Encrypt connection string
        var encryptedConnectionString = _aesEncryptor.Encrypt(request.ConnectionString);

        var connection = new DatabaseConnection
        {
            Name = request.Name,
            PlatformCode = request.PlatformCode,
            ConnectionString = encryptedConnectionString,
            DatabaseType = request.DatabaseType,
            Description = request.Description,
            IsProduction = request.IsProduction,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.DatabaseConnections.Add(connection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created database connection {ConnectionId} for platform {PlatformCode}",
            connection.Id, request.PlatformCode);

        return new DatabaseConnectionDto
        {
            Id = connection.Id,
            Name = connection.Name,
            PlatformCode = connection.PlatformCode,
            DatabaseType = connection.DatabaseType,
            Description = connection.Description,
            IsProduction = connection.IsProduction,
            IsActive = connection.IsActive
        };
    }

    public async Task<bool> UpdateConnectionAsync(int id, CreateConnectionRequest request)
    {
        var connection = await _context.DatabaseConnections.FindAsync(id);
        if (connection == null) return false;

        connection.Name = request.Name;
        connection.PlatformCode = request.PlatformCode;
        connection.ConnectionString = _aesEncryptor.Encrypt(request.ConnectionString);
        connection.DatabaseType = request.DatabaseType;
        connection.Description = request.Description;
        connection.IsProduction = request.IsProduction;
        connection.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated database connection {ConnectionId}", id);
        return true;
    }

    public async Task<bool> DeleteConnectionAsync(int id)
    {
        var connection = await _context.DatabaseConnections.FindAsync(id);
        if (connection == null) return false;

        // Soft delete
        connection.IsActive = false;
        connection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted database connection {ConnectionId}", id);
        return true;
    }

    public async Task<bool> TestConnectionAsync(int connectionId)
    {
        var connection = await _context.DatabaseConnections.FindAsync(connectionId);
        if (connection == null) return false;

        try
        {
            var connectionString = EnsureSslSettings(_aesEncryptor.Decrypt(connection.ConnectionString));
            using var sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connection test failed for connection {ConnectionId}", connectionId);
            return false;
        }
    }

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
