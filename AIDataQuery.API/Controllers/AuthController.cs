using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.Auth;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 认证控制器 - 处理用户登录、登出、密码修改等操作
/// </summary>
[Produces("application/json")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求，包含用户名和密码</param>
    /// <returns>登录成功返回 JWT Token 和用户信息</returns>
    /// <response code="200">登录成功</response>
    /// <response code="400">用户名或密码错误</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            return BadRequest(ApiResponse<LoginResponse>.Fail("用户名或密码错误"));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(result, "登录成功"));
    }

    /// <summary>
    /// 获取当前登录用户信息
    /// </summary>
    /// <returns>当前用户的详细信息，包括权限列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录或 Token 无效</response>
    /// <response code="404">用户不存在</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        var userInfo = await _authService.GetCurrentUserAsync(CurrentUserId);

        if (userInfo == null)
        {
            return NotFound(ApiResponse<UserInfo>.Fail("用户不存在"));
        }

        return Ok(ApiResponse<UserInfo>.Ok(userInfo));
    }

    /// <summary>
    /// 修改当前用户密码
    /// </summary>
    /// <param name="request">密码修改请求，包含当前密码和新密码</param>
    /// <returns>修改结果</returns>
    /// <response code="200">密码修改成功</response>
    /// <response code="400">当前密码错误</response>
    /// <response code="401">未登录或 Token 无效</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var success = await _authService.ChangePasswordAsync(CurrentUserId, request);

        if (!success)
        {
            return BadRequest(ApiResponse.Fail("当前密码错误"));
        }

        return Ok(ApiResponse.Ok("密码修改成功"));
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <returns>登出结果（客户端需清除本地 Token）</returns>
    /// <response code="200">登出成功</response>
    /// <response code="401">未登录或 Token 无效</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse> Logout()
    {
        // JWT is stateless, so we just return success
        // Client should remove the token
        return Ok(ApiResponse.Ok("登出成功"));
    }
}
