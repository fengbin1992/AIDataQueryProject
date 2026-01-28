using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.Platform;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 平台控制器 - 处理平台列表获取和数据库连接管理
/// </summary>
[Authorize]
[Produces("application/json")]
[Route("api/platforms")]
public class PlatformController : BaseController
{
    private readonly IPlatformService _platformService;

    public PlatformController(IPlatformService platformService)
    {
        _platformService = platformService;
    }

    /// <summary>
    /// 获取当前用户有权限的平台列表
    /// </summary>
    /// <returns>用户有权限访问的平台列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PlatformDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PlatformDto>>>> GetPlatforms()
    {
        var platforms = await _platformService.GetPlatformsAsync(CurrentUserId);
        return Ok(ApiResponse<List<PlatformDto>>.Ok(platforms));
    }

    /// <summary>
    /// 获取所有平台列表（管理员专用）
    /// </summary>
    /// <returns>所有平台列表，包括未激活的</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<PlatformDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<PlatformDto>>>> GetAllPlatforms()
    {
        var platforms = await _platformService.GetAllPlatformsAsync();
        return Ok(ApiResponse<List<PlatformDto>>.Ok(platforms));
    }

    /// <summary>
    /// 获取单个平台详情（管理员专用）
    /// </summary>
    /// <param name="id">平台ID</param>
    /// <returns>平台详情</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">平台不存在</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PlatformDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlatformDto>>> GetPlatform(int id)
    {
        var platform = await _platformService.GetPlatformByIdAsync(id);
        if (platform == null)
        {
            return NotFound(ApiResponse.Fail("平台不存在"));
        }
        return Ok(ApiResponse<PlatformDto>.Ok(platform));
    }

    /// <summary>
    /// 创建平台（管理员专用）
    /// </summary>
    /// <param name="request">平台创建请求</param>
    /// <returns>创建的平台信息</returns>
    /// <response code="200">创建成功</response>
    /// <response code="400">平台编码已存在或参数无效</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PlatformDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PlatformDto>>> CreatePlatform([FromBody] CreatePlatformRequest request)
    {
        var platform = await _platformService.CreatePlatformAsync(request);
        if (platform == null)
        {
            return BadRequest(ApiResponse.Fail("平台编码已存在"));
        }
        return Ok(ApiResponse<PlatformDto>.Ok(platform, "平台创建成功"));
    }

    /// <summary>
    /// 更新平台（管理员专用）
    /// </summary>
    /// <param name="id">平台ID</param>
    /// <param name="request">平台更新请求</param>
    /// <returns>更新结果</returns>
    /// <response code="200">更新成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">平台不存在</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> UpdatePlatform(int id, [FromBody] UpdatePlatformRequest request)
    {
        var success = await _platformService.UpdatePlatformAsync(id, request);
        if (!success)
        {
            return NotFound(ApiResponse.Fail("平台不存在"));
        }
        return Ok(ApiResponse.Ok("平台更新成功"));
    }

    /// <summary>
    /// 切换平台状态（管理员专用）
    /// </summary>
    /// <param name="id">平台ID</param>
    /// <returns>切换结果</returns>
    /// <response code="200">切换成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">平台不存在</response>
    [HttpPut("{id:int}/toggle-status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> TogglePlatformStatus(int id)
    {
        var success = await _platformService.TogglePlatformStatusAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse.Fail("平台不存在"));
        }
        return Ok(ApiResponse.Ok("平台状态已更新"));
    }

    /// <summary>
    /// 删除平台（管理员专用）
    /// </summary>
    /// <param name="id">平台ID</param>
    /// <returns>删除结果</returns>
    /// <response code="200">删除成功</response>
    /// <response code="400">平台存在关联数据，无法删除</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">平台不存在</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeletePlatform(int id)
    {
        var success = await _platformService.DeletePlatformAsync(id);
        if (!success)
        {
            return BadRequest(ApiResponse.Fail("平台不存在或存在关联的数据库连接，无法删除"));
        }
        return Ok(ApiResponse.Ok("平台已删除"));
    }

    /// <summary>
    /// 获取所有数据库连接列表（管理员专用，包含连接字符串）
    /// </summary>
    /// <returns>所有数据库连接列表，包含解密的连接字符串</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpGet("admin/connections")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<DatabaseConnectionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<DatabaseConnectionDto>>>> GetAllConnections()
    {
        var connections = await _platformService.GetAllConnectionsAsync();
        return Ok(ApiResponse<List<DatabaseConnectionDto>>.Ok(connections));
    }

    /// <summary>
    /// 获取单个数据库连接详情（管理员专用，包含连接字符串）
    /// </summary>
    /// <param name="id">连接ID</param>
    /// <returns>连接详情，包含解密的连接字符串</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">连接不存在</response>
    [HttpGet("connection/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DatabaseConnectionDto>>> GetConnection(int id)
    {
        var connection = await _platformService.GetConnectionByIdAsync(id);
        if (connection == null)
        {
            return NotFound(ApiResponse.Fail("连接不存在"));
        }
        return Ok(ApiResponse<DatabaseConnectionDto>.Ok(connection));
    }

    /// <summary>
    /// 获取指定平台下的数据库连接列表
    /// </summary>
    /// <param name="platformCode">平台编码</param>
    /// <returns>数据库连接列表（不包含连接字符串敏感信息）</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet("{platformCode}/connections")]
    [ProducesResponseType(typeof(ApiResponse<List<DatabaseConnectionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<DatabaseConnectionDto>>>> GetConnections(string platformCode)
    {
        var connections = await _platformService.GetConnectionsByPlatformAsync(platformCode, CurrentUserId);
        return Ok(ApiResponse<List<DatabaseConnectionDto>>.Ok(connections));
    }

    /// <summary>
    /// 创建数据库连接（管理员专用）
    /// </summary>
    /// <param name="request">连接创建请求，包含连接字符串（将被加密存储）</param>
    /// <returns>创建的连接信息</returns>
    /// <remarks>连接字符串会使用 AES-256 加密后存储到数据库</remarks>
    /// <response code="200">创建成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpPost("connections")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<DatabaseConnectionDto>>> CreateConnection([FromBody] CreateConnectionRequest request)
    {
        var connection = await _platformService.CreateConnectionAsync(request);
        return Ok(ApiResponse<DatabaseConnectionDto>.Ok(connection, "连接创建成功"));
    }

    /// <summary>
    /// 更新数据库连接（管理员专用）
    /// </summary>
    /// <param name="id">连接ID</param>
    /// <param name="request">连接更新请求</param>
    /// <returns>更新结果</returns>
    /// <response code="200">更新成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">连接不存在</response>
    [HttpPut("connections/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> UpdateConnection(int id, [FromBody] CreateConnectionRequest request)
    {
        var success = await _platformService.UpdateConnectionAsync(id, request);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("连接不存在"));
        }

        return Ok(ApiResponse.Ok("连接更新成功"));
    }

    /// <summary>
    /// 删除数据库连接（管理员专用，软删除）
    /// </summary>
    /// <param name="id">连接ID</param>
    /// <returns>删除结果</returns>
    /// <response code="200">删除成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">连接不存在</response>
    [HttpDelete("connections/{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteConnection(int id)
    {
        var success = await _platformService.DeleteConnectionAsync(id);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("连接不存在"));
        }

        return Ok(ApiResponse.Ok("连接已删除"));
    }

    /// <summary>
    /// 测试数据库连接（管理员专用）
    /// </summary>
    /// <param name="id">连接ID</param>
    /// <returns>连接测试结果</returns>
    /// <response code="200">测试完成，返回连接状态</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpPost("connections/{id}/test")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> TestConnection(int id)
    {
        var success = await _platformService.TestConnectionAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, success ? "连接测试成功" : "连接测试失败"));
    }
}
