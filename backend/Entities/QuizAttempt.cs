namespace LearnPath.API.Entities;

public class QuizAttempt
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int QuizId { get; set; }
    public int ModuleId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public int? TimeSpentSeconds { get; set; }
    public int? Score { get; set; }
    public decimal? Percentage { get; set; }
    public bool? Passed { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.Created;
    public int RandomSeed { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public Module Module { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<StudentAnswer> Answers { get; set; } = [];
}
