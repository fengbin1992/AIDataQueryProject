using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Template;

public class UpdateTemplateRequest
{
    public int? ModuleId { get; set; }

    [StringLength(100, ErrorMessage = "模板名称不能超过100个字符")]
    public string? Name { get; set; }

    public string? SqlContent { get; set; }

    [StringLength(500, ErrorMessage = "描述不能超过500个字符")]
    public string? Description { get; set; }

    public bool? IsPublic { get; set; }
}
