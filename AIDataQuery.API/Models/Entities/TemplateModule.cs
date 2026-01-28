namespace AIDataQuery.API.Models.Entities;

public class TemplateModule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TemplateModule? Parent { get; set; }
    public ICollection<TemplateModule> Children { get; set; } = new List<TemplateModule>();
    public ICollection<QueryTemplate> Templates { get; set; } = new List<QueryTemplate>();
}
