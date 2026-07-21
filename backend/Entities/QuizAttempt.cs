namespace LearnPath.API.Entities;

public enum AttemptStatus
{
    InProgress = 0,
    Completed = 1,
    TimedOut = 2
}

public class QuizAttempt
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int? Score { get; set; }
    public int? TotalPoints { get; set; }
    public bool? IsPassed { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? TimeLimitMinutes { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<QuizAnswer> Answers { get; set; } = [];
}
