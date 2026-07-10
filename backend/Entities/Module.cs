namespace LearnPath.API.Entities;

public class Module
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty; // video, article, quiz
    public string? ContentBody { get; set;  }
    public ModuleDifficulty Difficulty { get; set; } = ModuleDifficulty.Beginner;
    public int? EstimatedDurationMinutes { get; set; }
    public string? NotesHtml { get; set; }
    public string? PdfUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsDraft { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }
    public int Order { get; set; }
    public bool QuizEnabled { get; set; } = false;
    public int QuizQuestionCount { get; set; } = 0;
    public int QuizPassingScore { get; set; } = 0;
    public int? QuizTimeLimitMinutes { get; set; }
    public int LearningPathId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public LearningPath LearningPath { get; set; } = null!;
    public ICollection<ModuleDependency> Dependencies { get; set; } = [];
    public ICollection<ModuleDependency> Dependents { get; set; } = [];
    public ICollection<Progress> Progresses { get; set; } = [];
    public ICollection<ModuleResource> Resources { get; set; } = [];
    public ICollection<ModuleObjective> Objectives { get; set; } = [];
    public ICollection<ModuleTag> Tags { get; set; } = [];
}