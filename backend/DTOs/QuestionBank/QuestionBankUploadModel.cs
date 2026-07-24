using System.Text.Json.Serialization;

namespace LearnPath.API.DTOs.QuestionBank;

public class QuestionBankUploadModel
{
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public List<QuestionUploadModel> Questions { get; set; } = [];
}

public class QuestionUploadModel
{
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = [];

    [JsonPropertyName("correct_answer")]
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}
