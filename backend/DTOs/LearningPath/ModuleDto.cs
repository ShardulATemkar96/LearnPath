using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.LearningPath;

public class CreateModuleDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ContentBody { get; set; }
    public int Order { get; set; }
    public ModuleDifficulty Difficulty { get; set; } = ModuleDifficulty.Beginner;
    public int? EstimatedDurationMinutes { get; set; }
    public string? NotesHtml { get; set; }
    public string? PdfUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsDraft { get; set; }
    public bool QuizEnabled { get; set; }
    public int QuizQuestionCount { get; set; }
    public int QuizPassingScore { get; set; }
    public int? QuizTimeLimitMinutes { get; set; }
    public List<CreateResourceDto> Resources { get; set; } = [];
    public List<CreateObjectiveDto> Objectives { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public class UpdateModuleDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ContentBody { get; set; }
    public int Order { get; set; }
    public ModuleDifficulty Difficulty { get; set; } = ModuleDifficulty.Beginner;
    public int? EstimatedDurationMinutes { get; set; }
    public string? NotesHtml { get; set; }
    public string? PdfUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsDraft { get; set; }
    public bool QuizEnabled { get; set; }
    public int QuizQuestionCount { get; set; }
    public int QuizPassingScore { get; set; }
    public int? QuizTimeLimitMinutes { get; set; }
    public List<CreateResourceDto> Resources { get; set; } = [];
    public List<CreateObjectiveDto> Objectives { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public class ModuleResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? ContentBody { get; set; }
    public int Order { get; set; }
    public int LearningPathId { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsUnlocked { get; set; }
    public ModuleDifficulty Difficulty { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? NotesHtml { get; set; }
    public string? PdfUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool QuizEnabled { get; set; }
    public int? PreviousModuleId { get; set; }
    public int? NextModuleId { get; set; }
    public int QuizQuestionCount { get; set; }
    public int QuizPassingScore { get; set; }
    public int? QuizTimeLimitMinutes { get; set; }
    public List<ResourceDto> Resources { get; set; } = [];
    public List<ObjectiveDto> Objectives { get; set; } = [];
    public List<string> Tags { get; set; } = [];
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
public class CreateResourceDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class ResourceDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class CreateObjectiveDto
{
    public string ObjectiveText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class ObjectiveDto
{
    public int Id { get; set; }
    public string ObjectiveText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}