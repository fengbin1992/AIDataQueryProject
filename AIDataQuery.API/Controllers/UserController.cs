using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.User;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 用户管理控制器 - 管理员专用，用于用户的增删改查和权限管理
/// </summary>
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[Route("api/users")]
public class UserController : BaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 获取用户列表（分页）
    /// </summary>
    /// <param name="queryParams">分页和搜索参数</param>
    /// <returns>用户列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers([FromQuery] QueryParams queryParams)
    {
        var result = await _userService.GetUsersAsync(queryParams);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>用户详细信息</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">用户不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("用户不存在"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// 创建新用户
    /// </summary>
    /// <param name="request">用户创建请求</param>
    /// <returns>创建的用户信息</returns>
    /// <response code="201">创建成功</response>
    /// <response code="400">用户名已存在或参数无效</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id },
                ApiResponse<UserDto>.Ok(user, "用户创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="request">用户更新请求</param>
    /// <returns>更新后的用户信息</returns>
    /// <response code="200">更新成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">用户不存在</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateUserAsync(id, request);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("用户不存在"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user, "用户更新成功"));
    }

    /// <summary>
    /// 设置用户平台权限
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="platformCodes">平台编码列表</param>
    /// <returns>设置结果</returns>
    /// <response code="200">设置成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">用户不存在</response>
    [HttpPut("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> SetPermissions(int id, [FromBody] List<string> platformCodes)
    {
        var success = await _userService.SetUserPermissionsAsync(id, platformCodes);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("用户不存在"));
        }

        return Ok(ApiResponse.Ok("权限设置成功"));
    }

    /// <summary>
    /// 设置用户所有权限（平台权限和数据库连接权限）
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="request">权限设置请求</param>
    /// <returns>设置结果</returns>
    /// <response code="200">设置成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">用户不存在</response>
    [HttpPut("{id}/all-permissions")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> SetAllPermissions(int id, [FromBody] SetPermissionsRequest request)
    {
        var success = await _userService.SetUserAllPermissionsAsync(id, request.PlatformCodes, request.ConnectionIds);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("用户不存在"));
        }

        return Ok(ApiResponse.Ok("权限设置成功"));
    }

    /// <summary>
    /// 禁用用户（软删除）
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>操作结果</returns>
    /// <response code="200">禁用成功</response>
    /// <response code="401">未登录</response>
    /// <response code="403">无管理员权限</response>
    /// <response code="404">用户不存在</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("用户不存在"));
        }

        return Ok(ApiResponse.Ok("用户已禁用"));
    }
}
