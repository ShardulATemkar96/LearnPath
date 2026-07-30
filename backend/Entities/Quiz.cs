namespace LearnPath.API.Entities;

public class Quiz
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int QuestionBankId { get; set; }
    public int QuestionCount { get; set; }
    public Difficulty? DifficultyFilter { get; set; }
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Random;
    public int? TimeLimitMinutes { get; set; }
    public int PassingPercentage { get; set; } = 40;
    public int MaximumAttempts { get; set; } = 3;
    public QuizStatus Status { get; set; } = QuizStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public QuestionBank QuestionBank { get; set; } = null!;
    public ICollection<ModuleQuiz> ModuleQuizzes { get; set; } = [];
}
