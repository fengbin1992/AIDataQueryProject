using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.ConfigQuery;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Models.DTOs.Query;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 配置查询控制器 - 处理配置查询的增删改查和执行
/// </summary>
[Authorize]
[Produces("application/json")]
[Route("api/config-queries")]
public class ConfigQueryController : BaseController
{
    private readonly IConfigQueryService _configQueryService;

    public ConfigQueryController(IConfigQueryService configQueryService)
    {
        _configQueryService = configQueryService;
    }

    #region 配置查询 CRUD

    /// <summary>
    /// 获取配置查询列表
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="pageIndex">页码（从1开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <returns>分页的配置查询列表</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedListResponse<ConfigQueryListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedListResponse<ConfigQueryListItemDto>>>> GetList(
        [FromQuery] string? keyword = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _configQueryService.GetListAsync(CurrentUserId, IsAdmin, keyword, pageIndex, pageSize);
        return Ok(ApiResponse<PagedListResponse<ConfigQueryListItemDto>>.Ok(result));
    }

    /// <summary>
    /// 获取配置查询详情
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <returns>配置查询详情</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ConfigQueryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConfigQueryDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConfigQueryDetailDto>>> GetById(int id)
    {
        var result = await _configQueryService.GetByIdAsync(id, CurrentUserId, IsAdmin);

        if (result == null)
        {
            return NotFound(ApiResponse<ConfigQueryDetailDto>.Fail("配置查询不存在"));
        }

        return Ok(ApiResponse<ConfigQueryDetailDto>.Ok(result));
    }

    /// <summary>
    /// 创建配置查询
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建的配置查询ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateConfigQueryRequest request)
    {
        try
        {
            var id = await _configQueryService.CreateAsync(CurrentUserId, IsAdmin, request);
            return CreatedAtAction(nameof(GetById), new { id },
                ApiResponse<object>.Ok(new { id }, "配置查询创建成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 更新配置查询
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Update(int id, [FromBody] UpdateConfigQueryRequest request)
    {
        try
        {
            var success = await _configQueryService.UpdateAsync(id, CurrentUserId, IsAdmin, request);

            if (!success)
            {
                return NotFound(ApiResponse.Fail("配置查询不存在"));
            }

            return Ok(ApiResponse.Ok("配置查询更新成功"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 删除配置查询
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        try
        {
            var success = await _configQueryService.DeleteAsync(id, CurrentUserId, IsAdmin);

            if (!success)
            {
                return NotFound(ApiResponse.Fail("配置查询不存在"));
            }

            return Ok(ApiResponse.Ok("配置查询删除成功"));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// 复制配置查询
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <returns>复制后的配置查询ID</returns>
    [HttpPost("{id}/copy")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> Copy(int id)
    {
        try
        {
            var newId = await _configQueryService.CopyAsync(id, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = newId },
                ApiResponse<object>.Ok(new { id = newId }, "配置查询复制成功"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    #endregion

    #region 执行相关

    /// <summary>
    /// 执行配置查询
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <param name="request">执行请求</param>
    /// <returns>查询结果</returns>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(ApiResponse<QueryResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<QueryResult>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QueryResult>>> Execute(int id, [FromBody] ExecuteConfigQueryRequest request)
    {
        var result = await _configQueryService.ExecuteAsync(id, CurrentUserId, IsAdmin, request, ClientIp);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<QueryResult>.Fail(result.ErrorMessage ?? "执行失败"));
        }

        return Ok(ApiResponse<QueryResult>.Ok(result));
    }

    /// <summary>
    /// 解析SQL中的参数
    /// </summary>
    /// <param name="request">解析请求</param>
    /// <returns>参数列表</returns>
    [HttpPost("parse-params")]
    [ProducesResponseType(typeof(ApiResponse<ParseParamsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ParseParamsResponse>>> ParseParams([FromBody] ParseParamsRequest request)
    {
        var result = await _configQueryService.ParseParamsAsync(request.Sql);
        return Ok(ApiResponse<ParseParamsResponse>.Ok(result));
    }

    /// <summary>
    /// 获取动态选项
    /// </summary>
    /// <param name="request">获取选项请求</param>
    /// <returns>选项列表</returns>
    [HttpPost("options")]
    [ProducesResponseType(typeof(ApiResponse<GetOptionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetOptionsResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<GetOptionsResponse>>> GetOptions([FromBody] GetOptionsRequest request)
    {
        try
        {
            var result = await _configQueryService.GetOptionsAsync(CurrentUserId, IsAdmin, request);
            return Ok(ApiResponse<GetOptionsResponse>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<GetOptionsResponse>.Fail(ex.Message));
        }
    }

    #endregion

    #region 导入导出

    /// <summary>
    /// 导入配置
    /// </summary>
    /// <param name="request">导入请求</param>
    /// <returns>导入后的配置查询ID</returns>
    [HttpPost("import")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object>>> Import([FromBody] ImportConfigQueryRequest request)
    {
        try
        {
            var id = await _configQueryService.ImportAsync(CurrentUserId, IsAdmin, request.Json);
            return CreatedAtAction(nameof(GetById), new { id },
                ApiResponse<object>.Ok(new { id }, "配置导入成功"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail($"导入失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 导出配置
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <returns>配置JSON</returns>
    [HttpGet("{id}/export")]
    [ProducesResponseType(typeof(ApiResponse<ConfigQueryExportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ConfigQueryExportDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ConfigQueryExportDto>>> Export(int id)
    {
        try
        {
            var result = await _configQueryService.ExportAsync(id, CurrentUserId, IsAdmin);

            if (result == null)
            {
                return NotFound(ApiResponse<ConfigQueryExportDto>.Fail("配置查询不存在"));
            }

            return Ok(ApiResponse<ConfigQueryExportDto>.Ok(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    #endregion

    #region 参数预设

    /// <summary>
    /// 获取参数预设列表
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <returns>预设列表</returns>
    [HttpGet("{id}/presets")]
    [ProducesResponseType(typeof(ApiResponse<List<ConfigQueryParamPresetDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ConfigQueryParamPresetDto>>>> GetPresets(int id)
    {
        var result = await _configQueryService.GetPresetsAsync(id, CurrentUserId);
        return Ok(ApiResponse<List<ConfigQueryParamPresetDto>>.Ok(result));
    }

    /// <summary>
    /// 创建参数预设
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <param name="request">创建请求</param>
    /// <returns>创建的预设ID</returns>
    [HttpPost("{id}/presets")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<object>>> CreatePreset(int id, [FromBody] CreateParamPresetRequest request)
    {
        var presetId = await _configQueryService.CreatePresetAsync(id, CurrentUserId, request);
        return CreatedAtAction(nameof(GetPresets), new { id },
            ApiResponse<object>.Ok(new { id = presetId }, "预设创建成功"));
    }

    /// <summary>
    /// 更新参数预设
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <param name="presetId">预设ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}/presets/{presetId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> UpdatePreset(int id, int presetId, [FromBody] UpdateParamPresetRequest request)
    {
        var success = await _configQueryService.UpdatePresetAsync(id, presetId, CurrentUserId, request);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("预设不存在"));
        }

        return Ok(ApiResponse.Ok("预设更新成功"));
    }

    /// <summary>
    /// 删除参数预设
    /// </summary>
    /// <param name="id">配置查询ID</param>
    /// <param name="presetId">预设ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}/presets/{presetId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeletePreset(int id, int presetId)
    {
        var success = await _configQueryService.DeletePresetAsync(id, presetId, CurrentUserId);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("预设不存在"));
        }

        return Ok(ApiResponse.Ok("预设删除成功"));
    }

    #endregion
}
