using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.QuestionBank;

public class QuestionBankResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int Version { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public QuestionBankStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public class QuestionBankDetailDto : QuestionBankResponseDto
{
    public List<QuestionSummaryDto> Questions { get; set; } = [];
}

public class QuestionSummaryDto
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
}

public class QuestionBankUploadResult
{
    public bool Success { get; set; }
    public QuestionBankResponseDto? QuestionBank { get; set; }
    public List<ValidationError> Errors { get; set; } = [];
    public string? Message { get; set; }
}

public class ValidationError
{
    public string Reason { get; set; } = string.Empty;
    public int? QuestionIndex { get; set; }
    public string? Detail { get; set; }
}
