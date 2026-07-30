namespace LearnPath.API.Entities;

public class SubmissionAiFeedback
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string GrammarFeedback { get; set; } = string.Empty;
    public string RubricCoverage { get; set; } = string.Empty;
    public string MissingTopics { get; set; } = string.Empty;
    public int SuggestedScorePercentage { get; set; }
    public int SuggestedScoreMarks { get; set; }
    public string OverallRecommendation { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Submission Submission { get; set; } = null!;
}
