namespace LearnPath.API.DTOs.LearningPath;

public class CreateLearningPathDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPublic { get; set; } = false;
}