namespace LearnPath.API.DTOs.Progress;

public class MarkCompleteDto
{
    public int ModuleId { get; set; }
}

public class ProgressResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
    public string LearningPathTitle { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class PathProgressSummaryDto
{
    public int LearningPathId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalModules { get; set; }
    public int CompletedModules { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsCertificateEligible { get; set; }
    public List<ModuleProgressDto> Modules { get; set; } = [];
}

public class ModuleProgressDto
{
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? CompletedAt { get; set; }
}
