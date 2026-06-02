namespace LearnPath.API.Entities;

public class Module
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public string ContentType { get; set; } = string.Empty; // video, article, quiz
    public int Order { get; set; }
    public int LearningPathId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public LearningPath LearningPath { get; set; } = null!;
    public ICollection<ModuleDependency> Dependencies { get; set; } = [];
    public ICollection<ModuleDependency> Dependents { get; set; } = [];
    public ICollection<Progress> Progresses { get; set; } = [];
}