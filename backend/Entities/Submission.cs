namespace LearnPath.API.Entities;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public int? Grade { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Assignment Assignment { get; set; } = null!;
    public User User { get; set; } = null!;
}