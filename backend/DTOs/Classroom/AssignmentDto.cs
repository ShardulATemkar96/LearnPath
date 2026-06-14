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
    public DateTime CreatedAt { get; set; }
}

public class CreateSubmissionDto
{
    public string ContentUrl { get; set; } = string.Empty;
}

public class SubmissionResponseDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string ContentUrl { get; set; } = string.Empty;
    public string? Feedback { get; set; }
    public int? Grade { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class GradeSubmissionDto
{
    public int Grade { get; set; }
    public string? Feedback { get; set; }
}
