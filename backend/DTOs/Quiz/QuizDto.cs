using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.Quiz;

public class CreateQuizDto
{
    public string Title { get; set; } = string.Empty;
    public int QuestionBankId { get; set; }
    public int QuestionCount { get; set; }
    public Difficulty? DifficultyFilter { get; set; }
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Random;
    public int? TimeLimitMinutes { get; set; }
    public int PassingPercentage { get; set; } = 40;
    public int MaximumAttempts { get; set; } = 3;
}

public class UpdateQuizDto
{
    public string Title { get; set; } = string.Empty;
    public int QuestionBankId { get; set; }
    public int QuestionCount { get; set; }
    public Difficulty? DifficultyFilter { get; set; }
    public SelectionMode SelectionMode { get; set; } = SelectionMode.Random;
    public int? TimeLimitMinutes { get; set; }
    public int PassingPercentage { get; set; } = 40;
    public int MaximumAttempts { get; set; } = 3;
}

public class QuizResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int QuestionBankId { get; set; }
    public string QuestionBankTitle { get; set; } = string.Empty;
    public int QuestionBankVersion { get; set; }
    public int QuestionCount { get; set; }
    public Difficulty? DifficultyFilter { get; set; }
    public SelectionMode SelectionMode { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int PassingPercentage { get; set; }
    public int MaximumAttempts { get; set; }
    public QuizStatus Status { get; set; }
    public string? CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public class LinkQuizDto
{
    public int QuizId { get; set; }
}

public class ModuleQuizResponseDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string AssignedBy { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public bool Active { get; set; }
}
