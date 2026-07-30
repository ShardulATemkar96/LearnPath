namespace LearnPath.API.DTOs.Ai;

public class AiFeedbackResponseDto
{
    public string Summary { get; init; } = string.Empty;
    public string GrammarFeedback { get; init; } = string.Empty;
    public string RubricCoverage { get; init; } = string.Empty;
    public string MissingTopics { get; init; } = string.Empty;
    public SuggestedScoreDto SuggestedScore { get; init; } = new();
    public string OverallRecommendation { get; init; } = string.Empty;
    public string Disclaimer { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
}

public class SuggestedScoreDto
{
    public int Percentage { get; init; }
    public int Marks { get; init; }
}
