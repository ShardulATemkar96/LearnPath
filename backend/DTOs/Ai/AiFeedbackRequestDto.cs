namespace LearnPath.API.DTOs.Ai;

public class AiFeedbackRequestDto
{
    public int SubmissionId { get; init; }
    public string AssignmentQuestion { get; init; } = string.Empty;
    public string AssignmentDescription { get; init; } = string.Empty;
    public string AssignmentRubric { get; init; } = string.Empty;
    public string StudentSubmission { get; init; } = string.Empty;
}
