using Microsoft.EntityFrameworkCore;
using AIDataQuery.API.Data;
using AIDataQuery.API.Models.DTOs.Template;
using AIDataQuery.API.Models.Entities;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Services;

public class TemplateService : ITemplateService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(AppDbContext context, ILogger<TemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TemplateModuleDto>> GetModuleTreeAsync(int userId, bool isAdmin)
    {
        var modules = await _context.TemplateModules
            .Where(m => m.IsActive)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        // 所有用户只能看到公开的或自己创建的模板
        var templatesQuery = _context.QueryTemplates
            .Include(t => t.Module)
            .Include(t => t.Creator)
            .Where(t => t.IsPublic || t.CreatedBy == userId);

        var templates = await templatesQuery
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        return BuildModuleTree(modules, templates, null);
    }

    public async Task<TemplateModuleDto?> GetModuleByIdAsync(int id)
    {
        var module = await _context.TemplateModules
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (module == null) return null;

        return new TemplateModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            ParentId = module.ParentId,
            Icon = module.Icon,
            SortOrder = module.SortOrder
        };
    }

    public async Task<TemplateModuleDto> CreateModuleAsync(CreateModuleRequest request)
    {
        var module = new TemplateModule
        {
            Name = request.Name,
            ParentId = request.ParentId,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _context.TemplateModules.Add(module);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created module {ModuleId}: {ModuleName}", module.Id, module.Name);

        return new TemplateModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            ParentId = module.ParentId,
            Icon = module.Icon,
            SortOrder = module.SortOrder
        };
    }

    public async Task<TemplateModuleDto?> UpdateModuleAsync(int id, UpdateModuleRequest request)
    {
        var module = await _context.TemplateModules
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (module == null) return null;

        if (!string.IsNullOrEmpty(request.Name)) module.Name = request.Name;
        if (request.ParentId.HasValue) module.ParentId = request.ParentId.Value == 0 ? null : request.ParentId;
        if (request.Icon != null) module.Icon = request.Icon;
        if (request.SortOrder.HasValue) module.SortOrder = request.SortOrder.Value;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated module {ModuleId}", id);

        return new TemplateModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            ParentId = module.ParentId,
            Icon = module.Icon,
            SortOrder = module.SortOrder
        };
    }

    public async Task<bool> DeleteModuleAsync(int id)
    {
        var module = await _context.TemplateModules
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

        if (module == null) return false;

        // 检查是否有子模块
        var hasChildren = await _context.TemplateModules
            .AnyAsync(m => m.ParentId == id && m.IsActive);

        if (hasChildren)
        {
            throw new InvalidOperationException("无法删除包含子模块的模块，请先删除子模块");
        }

        // 检查是否有模板
        var hasTemplates = await _context.QueryTemplates
            .AnyAsync(t => t.ModuleId == id);

        if (hasTemplates)
        {
            throw new InvalidOperationException("无法删除包含模板的模块，请先删除或移动模板");
        }

        // 软删除
        module.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted module {ModuleId}", id);
        return true;
    }

    public async Task<List<TemplateDto>> GetTemplatesByModuleAsync(int moduleId, int userId, bool isAdmin)
    {
        // 所有用户只能看到公开的或自己创建的模板
        var query = _context.QueryTemplates
            .Include(t => t.Module)
            .Include(t => t.Creator)
            .Where(t => t.ModuleId == moduleId)
            .Where(t => t.IsPublic || t.CreatedBy == userId);

        var templates = await query
            .OrderBy(t => t.SortOrder)
            .ToListAsync();

        return templates.Select(MapToDto).ToList();
    }

    public async Task<TemplateDto?> GetTemplateByIdAsync(int id)
    {
        var template = await _context.QueryTemplates
            .Include(t => t.Module)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == id);

        return template == null ? null : MapToDto(template);
    }

    public async Task<TemplateDto> CreateTemplateAsync(int userId, bool isAdmin, CreateTemplateRequest request)
    {
        var template = new QueryTemplate
        {
            ModuleId = request.ModuleId,
            Name = request.Name,
            SqlContent = request.SqlContent,
            Description = request.Description,
            // 普通用户创建的模板强制为私有，只有管理员可以创建公开模板
            IsPublic = isAdmin ? request.IsPublic : false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.QueryTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created template {TemplateId} by user {UserId}", template.Id, userId);

        await _context.Entry(template).Reference(t => t.Module).LoadAsync();
        await _context.Entry(template).Reference(t => t.Creator).LoadAsync();

        return MapToDto(template);
    }

    public async Task<TemplateDto?> UpdateTemplateAsync(int id, int userId, bool isAdmin, UpdateTemplateRequest request)
    {
        var template = await _context.QueryTemplates
            .Include(t => t.Module)
            .Include(t => t.Creator)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return null;

        // Only creator or admin can update
        if (template.CreatedBy != userId && !isAdmin)
        {
            throw new UnauthorizedAccessException("只能修改自己创建的模板");
        }

        if (request.ModuleId.HasValue) template.ModuleId = request.ModuleId.Value;
        if (!string.IsNullOrEmpty(request.Name)) template.Name = request.Name;
        if (!string.IsNullOrEmpty(request.SqlContent)) template.SqlContent = request.SqlContent;
        if (request.Description != null) template.Description = request.Description;
        // 只有管理员可以修改公开状态
        if (request.IsPublic.HasValue && isAdmin) template.IsPublic = request.IsPublic.Value;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated template {TemplateId}", id);

        return MapToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(int id, int userId)
    {
        var template = await _context.QueryTemplates.FindAsync(id);
        if (template == null) return false;

        // Only creator or admin can delete
        if (template.CreatedBy != userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user?.Role != Models.Enums.UserRole.Admin)
            {
                throw new UnauthorizedAccessException("只能删除自己创建的模板");
            }
        }

        _context.QueryTemplates.Remove(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted template {TemplateId}", id);
        return true;
    }

    public async Task<List<TemplateDto>> SearchTemplatesAsync(string keyword, int userId, bool isAdmin)
    {
        // 所有用户只能搜索公开的或自己创建的模板
        var query = _context.QueryTemplates
            .Include(t => t.Module)
            .Include(t => t.Creator)
            .Where(t => t.Name.Contains(keyword) || (t.Description != null && t.Description.Contains(keyword)))
            .Where(t => t.IsPublic || t.CreatedBy == userId);

        var templates = await query
            .OrderBy(t => t.SortOrder)
            .Take(50)
            .ToListAsync();

        return templates.Select(MapToDto).ToList();
    }

    private List<TemplateModuleDto> BuildModuleTree(List<TemplateModule> modules, List<QueryTemplate> templates, int? parentId)
    {
        return modules
            .Where(m => m.ParentId == parentId)
            .Select(m => new TemplateModuleDto
            {
                Id = m.Id,
                Name = m.Name,
                ParentId = m.ParentId,
                Icon = m.Icon,
                SortOrder = m.SortOrder,
                Children = BuildModuleTree(modules, templates, m.Id),
                Templates = templates
                    .Where(t => t.ModuleId == m.Id)
                    .Select(MapToDto)
                    .ToList()
            })
            .ToList();
    }

    private static TemplateDto MapToDto(QueryTemplate template) => new()
    {
        Id = template.Id,
        ModuleId = template.ModuleId,
        ModuleName = template.Module?.Name ?? "",
        Name = template.Name,
        SqlContent = template.SqlContent,
        Description = template.Description,
        IsPublic = template.IsPublic,
        CreatedBy = template.CreatedBy,
        CreatedByName = template.Creator?.Nickname ?? "",
        CreatedAt = template.CreatedAt
    };
}
