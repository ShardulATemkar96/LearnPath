using LearnPath.API.DTOs.Ai;

namespace LearnPath.API.DTOs.Classroom;

public class CreateAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class UpdateAssignmentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class AssignmentResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int ClassroomId { get; set; }
    public int SubmissionCount { get; set; }
    public bool HasSubmitted { get; set; }
    public int? MySubmissionId { get; set; }
    public string? MyOriginalFileName { get; set; }
    public string? MySubmissionStatus { get; set; }
    public int? MyGrade { get; set; }
    public string? MyFeedback { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SubmissionResponseDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? FileExtension { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsLate { get; set; }
    public string Status { get; set; } = "NOT_SUBMITTED";
    public string? Feedback { get; set; }
    public int? Grade { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public AiFeedbackResponseDto? AiFeedback { get; set; }
}

public class GradeSubmissionDto
{
    public int Grade { get; set; }
    public string? Feedback { get; set; }
}

public class TransitionStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public class ReturnSubmissionDto
{
    public string? Feedback { get; set; }
}
