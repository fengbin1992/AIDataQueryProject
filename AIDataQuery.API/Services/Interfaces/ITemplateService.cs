using AIDataQuery.API.Models.DTOs.Template;

namespace AIDataQuery.API.Services.Interfaces;

public interface ITemplateService
{
    // 模块管理
    Task<List<TemplateModuleDto>> GetModuleTreeAsync(int userId, bool isAdmin);
    Task<TemplateModuleDto?> GetModuleByIdAsync(int id);
    Task<TemplateModuleDto> CreateModuleAsync(CreateModuleRequest request);
    Task<TemplateModuleDto?> UpdateModuleAsync(int id, UpdateModuleRequest request);
    Task<bool> DeleteModuleAsync(int id);

    // 模板管理
    Task<List<TemplateDto>> GetTemplatesByModuleAsync(int moduleId, int userId, bool isAdmin);
    Task<TemplateDto?> GetTemplateByIdAsync(int id);
    Task<TemplateDto> CreateTemplateAsync(int userId, bool isAdmin, CreateTemplateRequest request);
    Task<TemplateDto?> UpdateTemplateAsync(int id, int userId, bool isAdmin, UpdateTemplateRequest request);
    Task<bool> DeleteTemplateAsync(int id, int userId);
    Task<List<TemplateDto>> SearchTemplatesAsync(string keyword, int userId, bool isAdmin);
}
