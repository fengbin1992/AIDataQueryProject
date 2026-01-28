using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.User;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Models.Enums;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(QueryParams queryParams)
    {
        var query = _context.Users
            .Include(u => u.PlatformPermissions)
            .Include(u => u.ConnectionPermissions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Keyword))
        {
            query = query.Where(u =>
                u.Username.Contains(queryParams.Keyword) ||
                u.Nickname.Contains(queryParams.Keyword));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((queryParams.PageIndex - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageIndex = queryParams.PageIndex,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.PlatformPermissions)
            .Include(u => u.ConnectionPermissions)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("用户名已存在");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Nickname = request.Nickname,
            Email = request.Email,
            Role = request.Role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Add platform permissions
        if (request.PlatformCodes?.Any() == true)
        {
            foreach (var code in request.PlatformCodes)
            {
                _context.UserPlatformPermissions.Add(new UserPlatformPermission
                {
                    UserId = user.Id,
                    PlatformCode = code,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created user {Username} with ID {UserId}", user.Username, user.Id);

        // Reload with permissions
        await _context.Entry(user).Collection(u => u.PlatformPermissions).LoadAsync();
        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.PlatformPermissions)
            .Include(u => u.ConnectionPermissions)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return null;

        user.Nickname = request.Nickname;
        user.Email = request.Email;
        user.Role = request.Role;
        user.Status = request.Status;
        user.UpdatedAt = DateTime.UtcNow;

        // Update platform permissions if provided
        if (request.PlatformCodes != null)
        {
            // Remove existing permissions
            _context.UserPlatformPermissions.RemoveRange(user.PlatformPermissions);

            // Add new permissions
            foreach (var code in request.PlatformCodes)
            {
                _context.UserPlatformPermissions.Add(new UserPlatformPermission
                {
                    UserId = user.Id,
                    PlatformCode = code,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated user {UserId}", id);

        // Reload with permissions
        await _context.Entry(user).Collection(u => u.PlatformPermissions).LoadAsync();
        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        // Soft delete by disabling
        user.Status = UserStatus.Disabled;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Disabled user {UserId}", id);
        return true;
    }

    public async Task<bool> SetUserPermissionsAsync(int userId, List<string> platformCodes)
    {
        var user = await _context.Users
            .Include(u => u.PlatformPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        // Remove existing permissions
        _context.UserPlatformPermissions.RemoveRange(user.PlatformPermissions);

        // Add new permissions
        foreach (var code in platformCodes)
        {
            _context.UserPlatformPermissions.Add(new UserPlatformPermission
            {
                UserId = userId,
                PlatformCode = code,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated permissions for user {UserId}", userId);
        return true;
    }

    public async Task<bool> SetUserConnectionPermissionsAsync(int userId, List<int> connectionIds)
    {
        var user = await _context.Users
            .Include(u => u.ConnectionPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        // Remove existing connection permissions
        _context.UserConnectionPermissions.RemoveRange(user.ConnectionPermissions);

        // Add new connection permissions
        foreach (var connId in connectionIds)
        {
            _context.UserConnectionPermissions.Add(new UserConnectionPermission
            {
                UserId = userId,
                ConnectionId = connId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated connection permissions for user {UserId}", userId);
        return true;
    }

    public async Task<bool> SetUserAllPermissionsAsync(int userId, List<string> platformCodes, List<int> connectionIds)
    {
        var user = await _context.Users
            .Include(u => u.PlatformPermissions)
            .Include(u => u.ConnectionPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        // Remove existing permissions
        _context.UserPlatformPermissions.RemoveRange(user.PlatformPermissions);
        _context.UserConnectionPermissions.RemoveRange(user.ConnectionPermissions);

        // Add new platform permissions
        foreach (var code in platformCodes)
        {
            _context.UserPlatformPermissions.Add(new UserPlatformPermission
            {
                UserId = userId,
                PlatformCode = code,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Add new connection permissions
        foreach (var connId in connectionIds)
        {
            _context.UserConnectionPermissions.Add(new UserConnectionPermission
            {
                UserId = userId,
                ConnectionId = connId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated all permissions for user {UserId}", userId);
        return true;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Nickname = user.Nickname,
        Email = user.Email,
        Role = user.Role,
        Status = user.Status,
        ThemePreference = user.ThemePreference,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        PlatformCodes = user.PlatformPermissions.Select(p => p.PlatformCode).ToList(),
        ConnectionIds = user.ConnectionPermissions.Select(c => c.ConnectionId).ToList()
    };
}
