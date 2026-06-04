namespace LearnPath.API.Entities;

public class Certificate
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
    public string CertificateUrl { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public LearningPath LearningPath { get; set; } = null!;
}