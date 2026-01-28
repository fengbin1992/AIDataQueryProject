using AIDataQuery.API.Models.DTOs.User;
using AIDataQuery.API.Models.DTOs.Common;

namespace AIDataQuery.API.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(QueryParams queryParams);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task<UserDto?> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> SetUserPermissionsAsync(int userId, List<string> platformCodes);
    Task<bool> SetUserConnectionPermissionsAsync(int userId, List<int> connectionIds);
    Task<bool> SetUserAllPermissionsAsync(int userId, List<string> platformCodes, List<int> connectionIds);
}
