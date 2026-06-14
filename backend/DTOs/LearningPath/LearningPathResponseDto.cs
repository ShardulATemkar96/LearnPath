namespace LearnPath.API.DTOs.LearningPath;

public class LearningPathResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPublic { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int TotalModules { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class LearningPathDetailResponseDto : LearningPathResponseDto
{
    public List<ModuleResponseDto> Modules { get; set; } = [];
    public List<ModuleDependencyResponseDto> Dependencies { get; set; } = [];
}