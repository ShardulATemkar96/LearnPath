namespace LearnPath.API.Entities;
public class ModuleTag
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string TagName { get; set; } = string.Empty;

    public Module Module { get; set; } = null!;
}



