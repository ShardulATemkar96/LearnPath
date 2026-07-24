namespace LearnPath.API.Entities;

public class StudentAnswer
{
    public int Id { get; set; }
    public int QuizAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int OptionId { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    public QuizAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public Option Option { get; set; } = null!;
}
