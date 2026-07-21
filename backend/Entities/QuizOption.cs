namespace LearnPath.API.Entities;

public class QuizOption
{
    public int Id { get; set; }
    public int QuizQuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;
    public int OrderIndex { get; set; }

    public QuizQuestion Question { get; set; } = null!;
}
