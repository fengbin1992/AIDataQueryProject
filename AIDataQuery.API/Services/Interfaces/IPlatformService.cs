using AIDataQuery.API.Models.DTOs.Platform;

namespace AIDataQuery.API.Services.Interfaces;

public interface IPlatformService
{
    // 平台管理
    Task<List<PlatformDto>> GetPlatformsAsync(int userId);
    Task<List<PlatformDto>> GetAllPlatformsAsync();
    Task<PlatformDto?> GetPlatformByIdAsync(int id);
    Task<PlatformDto?> CreatePlatformAsync(CreatePlatformRequest request);
    Task<bool> UpdatePlatformAsync(int id, UpdatePlatformRequest request);
    Task<bool> TogglePlatformStatusAsync(int id);
    Task<bool> DeletePlatformAsync(int id);

    // 数据库连接管理
    Task<List<DatabaseConnectionDto>> GetConnectionsByPlatformAsync(string platformCode, int userId);
    Task<List<DatabaseConnectionDto>> GetAllConnectionsAsync();
    Task<DatabaseConnectionDto?> GetConnectionByIdAsync(int id);
    Task<DatabaseConnectionDto?> CreateConnectionAsync(CreateConnectionRequest request);
    Task<bool> UpdateConnectionAsync(int id, CreateConnectionRequest request);
    Task<bool> DeleteConnectionAsync(int id);
    Task<bool> TestConnectionAsync(int connectionId);
}
