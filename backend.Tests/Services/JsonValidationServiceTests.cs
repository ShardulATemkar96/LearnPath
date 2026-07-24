using FluentAssertions;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Services.Validation;
using Xunit;

namespace LearnPath.Tests.Services;

public class JsonValidationServiceTests
{
    private readonly IValidationService _sut = new JsonValidationService();

    /* ── File-level validation ─────────────────────────── */

    [Fact]
    public void Validate_NonJsonExtension_ReturnsError()
    {
        var result = _sut.Validate("test.xml", 100, "{}");
        result.Should().ContainSingle(e => e.Reason.Contains("JSON", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NoExtension_ReturnsError()
    {
        var result = _sut.Validate("test", 100, "{}");
        result.Should().ContainSingle(e => e.Reason.Contains("JSON", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_FileExceeds5Mb_ReturnsError()
    {
        var result = _sut.Validate("test.json", 6 * 1024 * 1024, "{}");
        result.Should().ContainSingle(e => e.Reason.Contains("5 MB", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InvalidJson_ReturnsError()
    {
        var result = _sut.Validate("test.json", 100, "{invalid}");
        result.Should().ContainSingle(e => e.Reason.Contains("JSON format", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NullModel_ReturnsError()
    {
        var result = _sut.Validate("test.json", 100, "null");
        result.Should().ContainSingle(e => e.Reason.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EmptyModel_ReturnsError()
    {
        var result = _sut.Validate("test.json", 100, "{}");
        result.Should().Contain(e => e.Reason.Contains("Title"));
        result.Should().Contain(e => e.Reason.Contains("Subject"));
        result.Should().Contain(e => e.Reason.Contains("Questions"));
    }

    [Fact]
    public void Validate_EmptyTitleAndSubject_ReturnsErrors()
    {
        var json = """{"title":"","subject":"","questions":[]}""";
        var result = _sut.Validate("test.json", 100, json);
        result.Should().Contain(e => e.Reason.Contains("Title"));
        result.Should().Contain(e => e.Reason.Contains("Subject"));
    }

    /* ── Per-question validation ───────────────────────── */

    [Fact]
    public void Validate_EmptyQuestionText_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "", "options": ["A","B"], "correct_answer": "A", "difficulty": "Easy", "explanation": "..." }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 1 && e.Reason.Contains("empty"));
    }

    [Fact]
    public void Validate_LessThanTwoOptions_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "Q1?", "options": ["A"], "correct_answer": "A", "difficulty": "Easy", "explanation": "..." }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 1 && e.Reason.Contains("2 options"));
    }

    [Fact]
    public void Validate_MissingCorrectAnswer_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "Q1?", "options": ["A","B"], "correct_answer": "", "difficulty": "Easy", "explanation": "..." }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 1 && e.Reason.Contains("Correct answer"));
    }

    [Fact]
    public void Validate_CorrectAnswerNotInOptions_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "Q1?", "options": ["A","B"], "correct_answer": "C", "difficulty": "Easy", "explanation": "..." }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 1 && e.Reason.Contains("not match"));
    }

    [Fact]
    public void Validate_InvalidDifficulty_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "Q1?", "options": ["A","B"], "correct_answer": "A", "difficulty": "Extreme", "explanation": "..." }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 1 && e.Reason.Contains("difficulty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingExplanation_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "Q1?", "options": ["A","B"], "correct_answer": "A", "difficulty": "Easy", "explanation": "" }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 1 && e.Reason.Contains("Explanation"));
    }

    [Fact]
    public void Validate_DuplicateQuestion_ReturnsError()
    {
        var json = """
            {
              "title": "Test",
              "subject": "Java",
              "questions": [
                { "question": "Same question?", "options": ["A","B"], "correct_answer": "A", "difficulty": "Easy", "explanation": "..." },
                { "question": "Same question?", "options": ["C","D"], "correct_answer": "C", "difficulty": "Medium", "explanation": "..." }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().Contain(e => e.QuestionIndex == 2 && e.Reason.Contains("Duplicate"));
    }

    /* ── Happy path ────────────────────────────────────── */

    [Fact]
    public void Validate_ValidJson_ReturnsNoErrors()
    {
        var json = """
            {
              "title": "Java OOP",
              "subject": "Java",
              "questions": [
                {
                  "question": "Which keyword inherits a class?",
                  "options": ["extends", "implements", "inherits", "super"],
                  "correct_answer": "extends",
                  "difficulty": "Easy",
                  "explanation": "Java uses extends for inheritance."
                }
              ]
            }
            """;
        var result = _sut.Validate("test.json", 500, json);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_DifficultyCaseInsensitive_AcceptsAllVariants()
    {
        foreach (var diff in new[] { "easy", "EASY", "Medium", "medium", "HARD", "Hard" })
        {
            var json = $$"""
                {
                  "title": "Test",
                  "subject": "Java",
                  "questions": [
                    { "question": "Q1?", "options": ["A","B"], "correct_answer": "A", "difficulty": "{{diff}}", "explanation": "..." }
                  ]
                }
                """;
            var result = _sut.Validate("test.json", 500, json);
            result.Should().BeEmpty($"difficulty '{diff}' should be valid");
        }
    }
}
