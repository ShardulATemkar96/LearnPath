namespace LearnPath.API.Entities;

public class Option
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }

    public Question Question { get; set; } = null!;
}
