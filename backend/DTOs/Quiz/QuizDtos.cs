using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.Quiz;

// ── Quiz ───────────────────────────────────────────────────────

public class CreateQuizDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShowResults { get; set; }
    public bool IsMandatory { get; set; }
}

public class QuizResponseDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? PassingScore { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShowResults { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsPublished { get; set; }
    public int QuestionCount { get; set; }
    public List<QuizQuestionResponseDto> Questions { get; set; } = [];
}

// ── Questions ──────────────────────────────────────────────────

public class CreateQuestionDto
{
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }
    public string? Explanation { get; set; }
    public List<CreateOptionDto> Options { get; set; } = [];
}

public class CreateOptionDto
{
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class QuizQuestionResponseDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public int Points { get; set; }
    public int OrderIndex { get; set; }
    public string? Explanation { get; set; }
    public List<QuizOptionResponseDto> Options { get; set; } = [];
}

public class QuizOptionResponseDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

// Admin variant — includes IsCorrect
public class AdminQuestionResponseDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public int Points { get; set; }
    public int OrderIndex { get; set; }
    public string? Explanation { get; set; }
    public List<AdminOptionResponseDto> Options { get; set; } = [];
}

public class AdminOptionResponseDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

// ── Attempts ───────────────────────────────────────────────────

public class StartAttemptResponseDto
{
    public int AttemptId { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public List<AttemptQuestionDto> Questions { get; set; } = [];
}

public class AttemptQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public int Points { get; set; }
    public int OrderIndex { get; set; }
    public List<AttemptOptionDto> Options { get; set; } = [];
}

public class AttemptOptionDto
{
    public int Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class SubmitAnswersDto
{
    public int AttemptId { get; set; }
    public List<AnswerSubmissionDto> Answers { get; set; } = [];
}

public class AnswerSubmissionDto
{
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? TextAnswer { get; set; }
}

public class AttemptResultDto
{
    public int AttemptId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public bool IsPassed { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int MaxAttempts { get; set; }
    public List<QuestionResultDto> QuestionResults { get; set; } = [];
}

public class QuestionResultDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; }
    public int Points { get; set; }
    public int PointsAwarded { get; set; }
    public bool IsCorrect { get; set; }
    public int? SelectedOptionId { get; set; }
    public int? CorrectOptionId { get; set; }
    public string? TextAnswer { get; set; }
    public string? CorrectAnswerText { get; set; }
    public string? Explanation { get; set; }
}

public class AttemptSummaryDto
{
    public int AttemptId { get; set; }
    public int Score { get; set; }
    public int TotalPoints { get; set; }
    public bool IsPassed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
