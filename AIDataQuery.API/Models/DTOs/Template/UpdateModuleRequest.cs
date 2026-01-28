using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Template;

public class UpdateModuleRequest
{
    [StringLength(50, ErrorMessage = "模块名称不能超过50个字符")]
    public string? Name { get; set; }

    public int? ParentId { get; set; }

    [StringLength(50)]
    public string? Icon { get; set; }

    public int? SortOrder { get; set; }
}
