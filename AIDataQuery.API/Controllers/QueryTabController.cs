using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.QueryTab;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 查询标签页控制器 - 管理用户的查询标签页
/// </summary>
[Authorize]
[Produces("application/json")]
[Route("api/query-tabs")]
public class QueryTabController : BaseController
{
    private readonly IQueryTabService _queryTabService;

    public QueryTabController(IQueryTabService queryTabService)
    {
        _queryTabService = queryTabService;
    }

    /// <summary>
    /// 获取当前用户的所有标签页
    /// </summary>
    /// <returns>标签页列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<QueryTabDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<QueryTabDto>>>> GetTabs()
    {
        var tabs = await _queryTabService.GetUserTabsAsync(CurrentUserId);
        return Ok(ApiResponse<List<QueryTabDto>>.Ok(tabs));
    }

    /// <summary>
    /// 获取单个标签页
    /// </summary>
    /// <param name="id">标签页ID</param>
    /// <returns>标签页详情</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    /// <response code="404">标签页不存在</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<QueryTabDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<QueryTabDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QueryTabDto>>> GetTab(int id)
    {
        var tab = await _queryTabService.GetTabByIdAsync(id, CurrentUserId);

        if (tab == null)
        {
            return NotFound(ApiResponse<QueryTabDto>.Fail("标签页不存在"));
        }

        return Ok(ApiResponse<QueryTabDto>.Ok(tab));
    }

    /// <summary>
    /// 创建标签页
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建的标签页</returns>
    /// <response code="201">创建成功</response>
    /// <response code="400">参数无效</response>
    /// <response code="401">未登录</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QueryTabDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<QueryTabDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryTabDto>>> CreateTab([FromBody] CreateQueryTabRequest request)
    {
        var tab = await _queryTabService.CreateTabAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetTab), new { id = tab.Id },
            ApiResponse<QueryTabDto>.Ok(tab, "标签页创建成功"));
    }

    /// <summary>
    /// 更新标签页
    /// </summary>
    /// <param name="id">标签页ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的标签页</returns>
    /// <response code="200">更新成功</response>
    /// <response code="401">未登录</response>
    /// <response code="404">标签页不存在</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<QueryTabDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<QueryTabDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QueryTabDto>>> UpdateTab(int id, [FromBody] UpdateQueryTabRequest request)
    {
        var tab = await _queryTabService.UpdateTabAsync(id, CurrentUserId, request);

        if (tab == null)
        {
            return NotFound(ApiResponse<QueryTabDto>.Fail("标签页不存在"));
        }

        return Ok(ApiResponse<QueryTabDto>.Ok(tab, "标签页更新成功"));
    }

    /// <summary>
    /// 删除标签页
    /// </summary>
    /// <param name="id">标签页ID</param>
    /// <returns>删除结果</returns>
    /// <response code="200">删除成功</response>
    /// <response code="401">未登录</response>
    /// <response code="404">标签页不存在</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTab(int id)
    {
        var success = await _queryTabService.DeleteTabAsync(id, CurrentUserId);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("标签页不存在"));
        }

        return Ok(ApiResponse.Ok("标签页删除成功"));
    }

    /// <summary>
    /// 调整标签页排序
    /// </summary>
    /// <param name="request">排序请求（按顺序排列的标签ID列表）</param>
    /// <returns>排序结果</returns>
    /// <response code="200">排序成功</response>
    /// <response code="400">参数无效</response>
    /// <response code="401">未登录</response>
    [HttpPut("reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> ReorderTabs([FromBody] ReorderQueryTabsRequest request)
    {
        var success = await _queryTabService.ReorderTabsAsync(CurrentUserId, request);

        if (!success)
        {
            return BadRequest(ApiResponse.Fail("排序失败：部分标签不存在或不属于当前用户"));
        }

        return Ok(ApiResponse.Ok("标签页排序成功"));
    }
}
