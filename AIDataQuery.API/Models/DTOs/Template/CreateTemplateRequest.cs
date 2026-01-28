using System.ComponentModel.DataAnnotations;

namespace AIDataQuery.API.Models.DTOs.Template;

public class CreateTemplateRequest
{
    [Required(ErrorMessage = "模块ID不能为空")]
    public int ModuleId { get; set; }

    [Required(ErrorMessage = "模板名称不能为空")]
    [StringLength(100, ErrorMessage = "模板名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "SQL内容不能为空")]
    public string SqlContent { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "描述不能超过500个字符")]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;
}
