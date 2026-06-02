namespace LearnPath.API.Entities;

public class ModuleDependency
{
    public int ModuleId { get; set; }
    public int DependsOnModuleId { get; set; }

    public Module Module { get; set; } = null!;
    public Module DependsOnModule { get; set; } = null!;
}