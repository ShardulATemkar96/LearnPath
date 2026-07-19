namespace LearnPath.API.Entities;
public class ModuleObjective
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string ObjectiveText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public Module Module { get; set; } = null!;
}

