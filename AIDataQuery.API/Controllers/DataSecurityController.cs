using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Models.DTOs.DataSecurity;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 数据安全控制器 - 管理脱敏规则和敏感字段标记
/// </summary>
[Authorize]
[Route("api/data-security")]
[Produces("application/json")]
public class DataSecurityController : BaseController
{
    private readonly IDataMaskingService _maskingService;

    public DataSecurityController(IDataMaskingService maskingService)
    {
        _maskingService = maskingService;
    }

    // ==================== 脱敏规则管理（管理员） ====================

    /// <summary>
    /// 获取脱敏规则列表
    /// </summary>
    [HttpGet("masking-rules")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<MaskingRuleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<MaskingRuleDto>>>> GetMaskingRules()
    {
        var rules = await _maskingService.GetMaskingRulesAsync();
        return Ok(ApiResponse<List<MaskingRuleDto>>.Ok(rules));
    }

    /// <summary>
    /// 获取脱敏规则详情
    /// </summary>
    [HttpGet("masking-rules/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MaskingRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MaskingRuleDto>>> GetMaskingRule(int id)
    {
        var rule = await _maskingService.GetMaskingRuleAsync(id);
        if (rule == null)
        {
            return NotFound(ApiResponse<MaskingRuleDto>.Fail("规则不存在"));
        }
        return Ok(ApiResponse<MaskingRuleDto>.Ok(rule));
    }

    /// <summary>
    /// 创建脱敏规则
    /// </summary>
    [HttpPost("masking-rules")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MaskingRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MaskingRuleDto>>> CreateMaskingRule([FromBody] CreateMaskingRuleRequest request)
    {
        var rule = await _maskingService.CreateMaskingRuleAsync(request);
        return Ok(ApiResponse<MaskingRuleDto>.Ok(rule, "创建成功"));
    }

    /// <summary>
    /// 更新脱敏规则
    /// </summary>
    [HttpPut("masking-rules/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<MaskingRuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MaskingRuleDto>>> UpdateMaskingRule(int id, [FromBody] UpdateMaskingRuleRequest request)
    {
        var rule = await _maskingService.UpdateMaskingRuleAsync(id, request);
        if (rule == null)
        {
            return NotFound(ApiResponse<MaskingRuleDto>.Fail("规则不存在"));
        }
        return Ok(ApiResponse<MaskingRuleDto>.Ok(rule, "更新成功"));
    }

    /// <summary>
    /// 删除脱敏规则
    /// </summary>
    [HttpDelete("masking-rules/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteMaskingRule(int id)
    {
        var success = await _maskingService.DeleteMaskingRuleAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Fail("规则不存在"));
        }
        return Ok(ApiResponse.Ok("删除成功"));
    }

    /// <summary>
    /// 切换脱敏规则启用状态
    /// </summary>
    [HttpPut("masking-rules/{id}/toggle")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> ToggleMaskingRule(int id)
    {
        var success = await _maskingService.ToggleMaskingRuleAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Fail("规则不存在"));
        }
        return Ok(ApiResponse.Ok("状态已切换"));
    }

    // ==================== 敏感字段标记（管理员） ====================

    /// <summary>
    /// 获取敏感字段标记列表
    /// </summary>
    [HttpGet("sensitive-fields")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<SensitiveFieldMarkDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SensitiveFieldMarkDto>>>> GetSensitiveFieldMarks([FromQuery] int? connectionId = null)
    {
        var marks = await _maskingService.GetSensitiveFieldMarksAsync(connectionId);
        return Ok(ApiResponse<List<SensitiveFieldMarkDto>>.Ok(marks));
    }

    /// <summary>
    /// 创建敏感字段标记
    /// </summary>
    [HttpPost("sensitive-fields")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<SensitiveFieldMarkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SensitiveFieldMarkDto>>> CreateSensitiveFieldMark([FromBody] CreateSensitiveFieldMarkRequest request)
    {
        try
        {
            var mark = await _maskingService.CreateSensitiveFieldMarkAsync(CurrentUserId, request);
            return Ok(ApiResponse<SensitiveFieldMarkDto>.Ok(mark, "创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 批量创建敏感字段标记
    /// </summary>
    [HttpPost("sensitive-fields/batch")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> BatchCreateSensitiveFieldMarks([FromBody] BatchCreateSensitiveFieldMarksRequest request)
    {
        var count = await _maskingService.BatchCreateSensitiveFieldMarksAsync(CurrentUserId, request);
        return Ok(ApiResponse<int>.Ok(count, $"成功标记 {count} 个字段"));
    }

    /// <summary>
    /// 删除敏感字段标记
    /// </summary>
    [HttpDelete("sensitive-fields/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteSensitiveFieldMark(int id)
    {
        var success = await _maskingService.DeleteSensitiveFieldMarkAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Fail("标记不存在"));
        }
        return Ok(ApiResponse.Ok("删除成功"));
    }

    /// <summary>
    /// 获取表结构并标识敏感字段
    /// </summary>
    [HttpGet("sensitive-fields/schema")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<TableSchemaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TableSchemaDto>>> GetTableSchemaWithSensitivity(
        [FromQuery] int connectionId,
        [FromQuery] string tableName)
    {
        var schema = await _maskingService.GetTableSchemaWithSensitivityAsync(connectionId, tableName);
        return Ok(ApiResponse<TableSchemaDto>.Ok(schema));
    }
}
