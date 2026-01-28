using AIDataQuery.API.Models.DTOs.Auth;

namespace AIDataQuery.API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<UserInfo?> GetCurrentUserAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task UpdateLastLoginAsync(int userId);
}
