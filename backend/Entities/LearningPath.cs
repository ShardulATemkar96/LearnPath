namespace LearnPath.API.Entities;

public class LearningPath
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool IsPublic { get; set; } = false;
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedBy { get; set; } = null!;
    public ICollection<Module> Modules { get; set; } = [];
    public ICollection<Classroom> Classrooms { get; set; } = [];
}