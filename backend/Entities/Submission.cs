namespace LearnPath.API.Entities;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? StoredFilePath { get; set; }
    public string? FileExtension { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsLate { get; set; }
    public string? Feedback { get; set; }
    public int? Grade { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    public Assignment Assignment { get; set; } = null!;
    public User User { get; set; } = null!;
}