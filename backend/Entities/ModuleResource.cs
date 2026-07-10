namespace LearnPath.API.Entities;
public class ModuleResource
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Type { get; set; } = string.Empty;// "video", "article", "pdf", "link"
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public Module Module { get; set; } = null!;
}

