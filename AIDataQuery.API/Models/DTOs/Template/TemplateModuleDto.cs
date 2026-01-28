namespace AIDataQuery.API.Models.DTOs.Template;

public class TemplateModuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public List<TemplateModuleDto> Children { get; set; } = new();
    public List<TemplateDto> Templates { get; set; } = new();
}
