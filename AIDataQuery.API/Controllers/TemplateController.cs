using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.Template;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 模板控制器 - 处理 SQL 查询模板的增删改查和模块管理
/// </summary>
[Authorize]
[Produces("application/json")]
[Route("api/templates")]
public class TemplateController : BaseController
{
    private readonly ITemplateService _templateService;

    public TemplateController(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <summary>
    /// 获取模板模块树
    /// </summary>
    /// <returns>模块树形结构（包含模板）</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet("modules")]
    [ProducesResponseType(typeof(ApiResponse<List<TemplateModuleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TemplateModuleDto>>>> GetModules()
    {
        var modules = await _templateService.GetModuleTreeAsync(CurrentUserId, IsAdmin);
        return Ok(ApiResponse<List<TemplateModuleDto>>.Ok(modules));
    }

    /// <summary>
    /// 创建模块
    /// </summary>
    /// <param name="request">模块创建请求</param>
    /// <returns>创建的模块信息</returns>
    /// <response code="201">创建成功</response>
    /// <response code="400">参数无效</response>
    /// <response code="401">未登录</response>
    [HttpPost("modules")]
    [ProducesResponseType(typeof(ApiResponse<TemplateModuleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TemplateModuleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TemplateModuleDto>>> CreateModule([FromBody] CreateModuleRequest request)
    {
        var module = await _templateService.CreateModuleAsync(request);
        return CreatedAtAction(nameof(GetModules), ApiResponse<TemplateModuleDto>.Ok(module, "模块创建成功"));
    }

    /// <summary>
    /// 更新模块
    /// </summary>
    /// <param name="id">模块ID</param>
    /// <param name="request">模块更新请求</param>
    /// <returns>更新后的模块信息</returns>
    /// <response code="200">更新成功</response>
    /// <response code="401">未登录</response>
    /// <response code="404">模块不存在</response>
    [HttpPut("modules/{id}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateModuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TemplateModuleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemplateModuleDto>>> UpdateModule(int id, [FromBody] UpdateModuleRequest request)
    {
        var module = await _templateService.UpdateModuleAsync(id, request);

        if (module == null)
        {
            return NotFound(ApiResponse<TemplateModuleDto>.Fail("模块不存在"));
        }

        return Ok(ApiResponse<TemplateModuleDto>.Ok(module, "模块更新成功"));
    }

    /// <summary>
    /// 删除模块
    /// </summary>
    /// <param name="id">模块ID</param>
    /// <returns>删除结果</returns>
    /// <response code="200">删除成功</response>
    /// <response code="400">模块包含子模块或模板</response>
    /// <response code="401">未登录</response>
    /// <response code="404">模块不存在</response>
    [HttpDelete("modules/{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteModule(int id)
    {
        try
        {
            var success = await _templateService.DeleteModuleAsync(id);

            if (!success)
            {
                return NotFound(ApiResponse.Fail("模块不存在"));
            }

            return Ok(ApiResponse.Ok("模块删除成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取指定模块下的模板列表
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <returns>模板列表（包含公开模板和用户自己的私有模板）</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet("module/{moduleId}")]
    [ProducesResponseType(typeof(ApiResponse<List<TemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TemplateDto>>>> GetTemplatesByModule(int moduleId)
    {
        var templates = await _templateService.GetTemplatesByModuleAsync(moduleId, CurrentUserId, IsAdmin);
        return Ok(ApiResponse<List<TemplateDto>>.Ok(templates));
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    /// <param name="id">模板ID</param>
    /// <returns>模板详细信息，包含SQL内容</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="404">模板不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> GetTemplate(int id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);

        if (template == null)
        {
            return NotFound(ApiResponse<TemplateDto>.Fail("模板不存在"));
        }

        return Ok(ApiResponse<TemplateDto>.Ok(template));
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    /// <param name="request">模板创建请求</param>
    /// <returns>创建的模板信息</returns>
    /// <response code="201">创建成功</response>
    /// <response code="400">参数无效</response>
    /// <response code="401">未登录</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var template = await _templateService.CreateTemplateAsync(CurrentUserId, IsAdmin, request);
        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id },
            ApiResponse<TemplateDto>.Ok(template, "模板创建成功"));
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    /// <param name="id">模板ID</param>
    /// <param name="request">模板更新请求</param>
    /// <returns>更新后的模板信息</returns>
    /// <remarks>只能更新自己创建的模板，管理员可以更新所有模板</remarks>
    /// <response code="200">更新成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无权限修改此模板</response>
    /// <response code="404">模板不存在</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TemplateDto>>> UpdateTemplate(int id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var template = await _templateService.UpdateTemplateAsync(id, CurrentUserId, IsAdmin, request);

            if (template == null)
            {
                return NotFound(ApiResponse<TemplateDto>.Fail("模板不存在"));
            }

            return Ok(ApiResponse<TemplateDto>.Ok(template, "模板更新成功"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    /// <param name="id">模板ID</param>
    /// <returns>删除结果</returns>
    /// <remarks>只能删除自己创建的模板，管理员可以删除所有模板</remarks>
    /// <response code="200">删除成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无权限删除此模板</response>
    /// <response code="404">模板不存在</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTemplate(int id)
    {
        try
        {
            var success = await _templateService.DeleteTemplateAsync(id, CurrentUserId);

            if (!success)
            {
                return NotFound(ApiResponse.Fail("模板不存在"));
            }

            return Ok(ApiResponse.Ok("模板删除成功"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// 搜索模板
    /// </summary>
    /// <param name="keyword">搜索关键词（匹配模板名称或描述）</param>
    /// <returns>匹配的模板列表（最多50条）</returns>
    /// <response code="200">搜索成功</response>
    /// <response code="401">未登录</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<TemplateDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TemplateDto>>>> SearchTemplates([FromQuery] string keyword)
    {
        var templates = await _templateService.SearchTemplatesAsync(keyword, CurrentUserId, IsAdmin);
        return Ok(ApiResponse<List<TemplateDto>>.Ok(templates));
    }
}
