namespace LearnPath.API.DTOs.LearningPath;

public class CreateModuleDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class UpdateModuleDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class ModuleResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int Order { get; set; }
    public int LearningPathId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsUnlocked { get; set; }
}

public class ModuleDependencyResponseDto
{
    public int ModuleId { get; set; }
    public int DependsOnModuleId { get; set; }
}

public class AddDependencyDto
{
    public int ModuleId { get; set; }
    public int DependsOnModuleId { get; set; }
}