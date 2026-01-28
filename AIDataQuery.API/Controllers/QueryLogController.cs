using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.QueryLog;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 查询日志控制器 - 查看SQL查询执行历史记录
/// </summary>
[Authorize]
[Produces("application/json")]
[Route("api/query-logs")]
public class QueryLogController : BaseController
{
    private readonly IQueryLogService _queryLogService;

    public QueryLogController(IQueryLogService queryLogService)
    {
        _queryLogService = queryLogService;
    }

    /// <summary>
    /// 获取查询历史（普通用户只能看自己的记录）
    /// </summary>
    /// <param name="queryParams">分页和筛选参数</param>
    /// <returns>查询历史记录列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QueryLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<QueryLogDto>>>> GetLogs([FromQuery] QueryLogParams queryParams)
    {
        var result = await _queryLogService.GetLogsAsync(CurrentUserId, IsAdmin, queryParams);
        return Ok(ApiResponse<PagedResult<QueryLogDto>>.Ok(result));
    }

    /// <summary>
    /// 获取所有用户的查询历史（管理员专用）
    /// </summary>
    /// <param name="queryParams">分页和筛选参数</param>
    /// <returns>所有用户的查询历史记录</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QueryLogDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<QueryLogDto>>>> GetAllLogs([FromQuery] QueryLogParams queryParams)
    {
        var result = await _queryLogService.GetLogsAsync(CurrentUserId, true, queryParams);
        return Ok(ApiResponse<PagedResult<QueryLogDto>>.Ok(result));
    }

    /// <summary>
    /// 获取查询历史详情
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <returns>查询历史详细信息，包含完整SQL</returns>
    /// <remarks>普通用户只能查看自己的记录，管理员可查看所有记录</remarks>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="404">记录不存在或无权限查看</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<QueryLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<QueryLogDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QueryLogDto>>> GetLog(long id)
    {
        var log = await _queryLogService.GetLogByIdAsync(id, CurrentUserId, IsAdmin);

        if (log == null)
        {
            return NotFound(ApiResponse<QueryLogDto>.Fail("记录不存在"));
        }

        return Ok(ApiResponse<QueryLogDto>.Ok(log));
    }
}
