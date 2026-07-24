using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.Attempt;

public class AttemptStartResponseDto
{
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public int AttemptNumber { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int? TimeSpentSeconds { get; set; }
    public List<AttemptQuestionDto> Questions { get; set; } = [];
}

public class AttemptQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int? SelectedOptionId { get; set; }
    public List<AttemptOptionDto> Options { get; set; } = [];
}

public class AttemptOptionDto
{
    public int OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class SaveAnswerRequestDto
{
    public int QuestionId { get; set; }
    public int OptionId { get; set; }
}

public class SubmitResponseDto
{
    public int AttemptId { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Percentage { get; set; }
    public bool Passed { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int AttemptNumber { get; set; }
    public int PassingPercentage { get; set; }
}

public class ReviewResponseDto
{
    public int AttemptId { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Percentage { get; set; }
    public bool Passed { get; set; }
    public int TimeSpentSeconds { get; set; }
    public int AttemptNumber { get; set; }
    public List<ReviewQuestionDto> Questions { get; set; } = [];
}

public class ReviewQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public int? SelectedOptionId { get; set; }
    public int CorrectOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public List<ReviewOptionDto> Options { get; set; } = [];
}

public class ReviewOptionDto
{
    public int OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsSelected { get; set; }
    public int DisplayOrder { get; set; }
}
