using System.Text.Json;
using LearnPath.API.DTOs.QuestionBank;
using LearnPath.API.Interfaces.Services;

namespace LearnPath.API.Services.Validation;

public class JsonValidationService : IValidationService
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> ValidDifficulties = ["easy", "medium", "hard"];

    public List<ValidationError> Validate(string fileName, long fileSize, string jsonContent)
    {
        var errors = new List<ValidationError>();

        // Step 1: Extension check
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (ext != ".json")
        {
            errors.Add(new ValidationError
            {
                Reason = "Invalid file format. Only JSON files are accepted.",
                Detail = $"Got: {ext ?? "(no extension)"}"
            });
            return errors;
        }

        // Step 2: File size
        if (fileSize > MaxFileSize)
        {
            errors.Add(new ValidationError
            {
                Reason = "File exceeds maximum size of 5 MB.",
                Detail = $"Size: {fileSize} bytes"
            });
            return errors;
        }

        // Step 3: JSON parsing
        QuestionBankUploadModel? model;
        try
        {
            model = JsonSerializer.Deserialize<QuestionBankUploadModel>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            errors.Add(new ValidationError
            {
                Reason = "Invalid JSON format.",
                Detail = ex.Message
            });
            return errors;
        }

        if (model is null)
        {
            errors.Add(new ValidationError { Reason = "Uploaded JSON is empty or could not be parsed." });
            return errors;
        }

        // Step 4: Required fields
        if (string.IsNullOrWhiteSpace(model.Title))
            errors.Add(new ValidationError { Reason = "Title is required." });

        if (string.IsNullOrWhiteSpace(model.Subject))
            errors.Add(new ValidationError { Reason = "Subject is required." });

        if (model.Questions is null || model.Questions.Count == 0)
            errors.Add(new ValidationError { Reason = "Questions array is missing or empty." });

        if (errors.Count > 0)
            return errors;

        // Step 5-8: Per-question validation
        var seenQuestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < model.Questions!.Count; i++)
        {
            var q = model.Questions[i];

            // Question text
            if (string.IsNullOrWhiteSpace(q.Question))
                errors.Add(new ValidationError { Reason = "Question text is empty.", QuestionIndex = i + 1 });

            // Options
            if (q.Options is null || q.Options.Count < 2)
                errors.Add(new ValidationError { Reason = "Question must have at least 2 options.", QuestionIndex = i + 1 });

            // Correct answer
            if (string.IsNullOrWhiteSpace(q.CorrectAnswer))
                errors.Add(new ValidationError { Reason = "Correct answer is missing.", QuestionIndex = i + 1 });

            // Correct answer exists in options
            if (q.Options is not null && !string.IsNullOrWhiteSpace(q.CorrectAnswer))
            {
                if (!q.Options.Any(o => o.Trim().Equals(q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase)))
                    errors.Add(new ValidationError { Reason = "Correct answer does not match any option.", QuestionIndex = i + 1 });
            }

            // More than one correct answer check (implied by single correct_answer field)

            // Difficulty
            if (string.IsNullOrWhiteSpace(q.Difficulty) || !ValidDifficulties.Contains(q.Difficulty.Trim().ToLowerInvariant()))
                errors.Add(new ValidationError { Reason = "Invalid difficulty. Must be Easy, Medium, or Hard.", QuestionIndex = i + 1, Detail = $"Got: {q.Difficulty}" });

            // Explanation
            if (string.IsNullOrWhiteSpace(q.Explanation))
                errors.Add(new ValidationError { Reason = "Explanation is missing.", QuestionIndex = i + 1 });

            // Duplicate detection
            if (!string.IsNullOrWhiteSpace(q.Question) && !seenQuestions.Add(q.Question.Trim()))
                errors.Add(new ValidationError { Reason = "Duplicate question detected.", QuestionIndex = i + 1 });
        }

        return errors;
    }
}
