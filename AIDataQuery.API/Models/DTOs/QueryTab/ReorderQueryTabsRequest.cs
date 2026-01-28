using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.QueryTab;

/// <summary>
/// 调整标签页排序请求
/// </summary>
public class ReorderQueryTabsRequest
{
    [Required(ErrorMessage = "标签ID列表不能为空")]
    public List<int> TabIds { get; set; } = new();
}
