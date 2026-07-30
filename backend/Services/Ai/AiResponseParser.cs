using System.Text.RegularExpressions;
using LearnPath.API.DTOs.Ai;

namespace LearnPath.API.Services.Ai;

public class AiResponseParser
{
    private static readonly string Disclaimer =
        "This feedback is AI-generated and is intended only to assist the instructor. " +
        "The final evaluation is determined solely by the instructor.";

    public AiFeedbackResponseDto Parse(string rawResponse)
    {
        return new AiFeedbackResponseDto
        {
            Summary = ExtractSection(rawResponse, "Summary"),
            GrammarFeedback = ExtractSection(rawResponse, "Grammar & Language Feedback"),
            RubricCoverage = ExtractSection(rawResponse, "Rubric Coverage"),
            MissingTopics = ExtractSection(rawResponse, "Missing Topics"),
            SuggestedScore = ParseSuggestedScore(rawResponse),
            OverallRecommendation = ExtractSection(rawResponse, "Overall Recommendation"),
            Disclaimer = Disclaimer,
            GeneratedAt = DateTime.UtcNow,
        };
    }

    private static string ExtractSection(string text, string sectionName)
    {
        var pattern = $@"(?<=^##\s*{Regex.Escape(sectionName)}\s*\n)(.+?)(?=\n##\s|\n*$)";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
        if (!match.Success)
        {
            pattern = $@"(?<=^#\s*{Regex.Escape(sectionName)}\s*\n)(.+?)(?=\n#\s|\n*$)";
            match = Regex.Match(text, pattern, RegexOptions.Singleline | RegexOptions.Multiline);
        }
        return match.Success ? match.Value.Trim() : string.Empty;
    }

    private static SuggestedScoreDto ParseSuggestedScore(string text)
    {
        var percentage = 0;
        var marks = 0;
        var percentageMatch = Regex.Match(text, @"Percentage[:\s]*(\d+)", RegexOptions.IgnoreCase);
        if (percentageMatch.Success)
            int.TryParse(percentageMatch.Groups[1].Value, out percentage);

        var marksMatch = Regex.Match(text, @"Marks[:\s]*(\d+)", RegexOptions.IgnoreCase);
        if (marksMatch.Success)
            int.TryParse(marksMatch.Groups[1].Value, out marks);

        return new SuggestedScoreDto { Percentage = percentage, Marks = marks };
    }
}
