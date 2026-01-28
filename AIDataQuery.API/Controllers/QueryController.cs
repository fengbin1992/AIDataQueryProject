using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIDataQuery.API.Models.DTOs.Query;
using AIDataQuery.API.Models.DTOs.Common;
using AIDataQuery.API.Services.Interfaces;

namespace AIDataQuery.API.Controllers;

/// <summary>
/// 查询控制器 - 处理 SQL 查询执行、表结构获取、数据导出等操作
/// </summary>
[Authorize]
[Produces("application/json")]
public class QueryController : BaseController
{
    private readonly IQueryService _queryService;

    public QueryController(IQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>
    /// 执行 SQL 查询
    /// </summary>
    /// <param name="request">查询请求，包含平台编码、连接ID和SQL语句</param>
    /// <returns>查询结果，包含列名和数据行</returns>
    /// <remarks>
    /// 注意事项：
    /// - 仅支持 SELECT 语句
    /// - 查询超时时间为 30 秒
    /// - 单次查询最多返回 10000 行
    /// </remarks>
    /// <response code="200">查询成功</response>
    /// <response code="400">SQL 语法错误或包含禁止的关键字</response>
    /// <response code="401">未登录</response>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ApiResponse<QueryResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<QueryResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<QueryResult>>> Execute([FromBody] QueryRequest request)
    {
        var result = await _queryService.ExecuteQueryAsync(CurrentUserId, request, ClientIp);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<QueryResult>.Fail(result.ErrorMessage ?? "查询失败"));
        }

        return Ok(ApiResponse<QueryResult>.Ok(result));
    }

    /// <summary>
    /// 获取数据库表列表
    /// </summary>
    /// <param name="connectionId">数据库连接ID</param>
    /// <returns>表名列表</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet("tables")]
    [ProducesResponseType(typeof(ApiResponse<List<TableInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TableInfo>>>> GetTables([FromQuery] int connectionId)
    {
        var tables = await _queryService.GetTablesAsync(connectionId);
        return Ok(ApiResponse<List<TableInfo>>.Ok(tables));
    }

    /// <summary>
    /// 获取表字段列表
    /// </summary>
    /// <param name="connectionId">数据库连接ID</param>
    /// <param name="tableName">表名</param>
    /// <returns>字段信息列表，包含字段名、数据类型、是否可空</returns>
    /// <response code="200">获取成功</response>
    /// <response code="401">未登录</response>
    [HttpGet("columns")]
    [ProducesResponseType(typeof(ApiResponse<List<ColumnInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ColumnInfo>>>> GetColumns(
        [FromQuery] int connectionId,
        [FromQuery] string tableName)
    {
        var columns = await _queryService.GetColumnsAsync(connectionId, tableName);
        return Ok(ApiResponse<List<ColumnInfo>>.Ok(columns));
    }

    /// <summary>
    /// 导出查询结果
    /// </summary>
    /// <param name="request">导出请求，包含SQL和导出格式（csv/excel）</param>
    /// <returns>导出文件流</returns>
    /// <remarks>
    /// 支持的导出格式：
    /// - csv: 逗号分隔文本文件
    /// - excel: Excel 2007+ 格式（.xlsx）
    ///
    /// 最大导出行数为 50000 行
    /// </remarks>
    /// <response code="200">导出成功，返回文件流</response>
    /// <response code="400">导出失败</response>
    /// <response code="401">未登录</response>
    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Export([FromBody] ExportRequest request)
    {
        try
        {
            if (request.Format.ToLower() == "excel")
            {
                var bytes = await _queryService.ExportToExcelAsync(new QueryRequest
                {
                    PlatformCode = request.PlatformCode,
                    ConnectionId = request.ConnectionId,
                    Sql = request.Sql
                });

                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"export_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
            }
            else
            {
                var bytes = await _queryService.ExportToCsvAsync(new QueryRequest
                {
                    PlatformCode = request.PlatformCode,
                    ConnectionId = request.ConnectionId,
                    Sql = request.Sql
                });

                return File(bytes, "text/csv", $"export_{DateTime.Now:yyyyMMddHHmmss}.csv");
            }
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <param name="connectionId">数据库连接ID</param>
    /// <returns>连接测试结果</returns>
    /// <response code="200">测试完成，返回连接状态</response>
    /// <response code="401">未登录</response>
    [HttpPost("test-connection")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> TestConnection([FromQuery] int connectionId)
    {
        var success = await _queryService.TestConnectionAsync(connectionId);
        return Ok(ApiResponse<bool>.Ok(success, success ? "连接成功" : "连接失败"));
    }
}
