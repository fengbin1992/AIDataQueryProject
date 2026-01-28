namespace AIDataQuery.API.Models.DTOs.Template;

public class TemplateDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SqlContent { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
