using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.Auth;
using AIDataQuery.API.Models.Enums;
using AIDataQuery.API.Services.Interfaces;
using AIDataQuery.API.Infrastructure.Security;

namespace AIDataQuery.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.PlatformPermissions)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User {Username} not found", request.Username);
            return null;
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("Login failed: User {Username} is disabled", request.Username);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for user {Username}", request.Username);
            return null;
        }

        var expireHours = request.RememberMe ? 24 * 30 : 8; // 30 days if remember me
        var token = _jwtTokenGenerator.GenerateToken(user.Id, user.Username, user.Role.ToString(), expireHours);

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Username} logged in successfully", request.Username);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(expireHours),
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                Email = user.Email,
                Role = user.Role.ToString(),
                ThemePreference = user.ThemePreference,
                Platforms = user.PlatformPermissions.Select(p => p.PlatformCode).ToList()
            }
        };
    }

    public async Task<UserInfo?> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.PlatformPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Email = user.Email,
            Role = user.Role.ToString(),
            ThemePreference = user.ThemePreference,
            Platforms = user.PlatformPermissions.Select(p => p.PlatformCode).ToList()
        };
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed password", userId);
        return true;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
