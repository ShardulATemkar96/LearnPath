namespace LearnPath.API.Entities;

public class QuizQuestion
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuizQuestionType QuestionType { get; set; } = QuizQuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public int OrderIndex { get; set; }
    public string? Explanation { get; set; }

    public Quiz Quiz { get; set; } = null!;
    public ICollection<QuizOption> Options { get; set; } = [];
    public ICollection<QuizAnswer> Answers { get; set; } = [];
}
