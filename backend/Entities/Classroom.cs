namespace LearnPath.API.Entities;

public class Classroom
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public LearningPath LearningPath { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<UserClassroom> UserClassrooms { get; set; } = [];
    public ICollection<Assignment> Assignments { get; set; } = [];
}