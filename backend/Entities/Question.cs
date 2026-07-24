namespace LearnPath.API.Entities;

public class Question
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; } = Difficulty.Easy;
    public string? Explanation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public QuestionBank QuestionBank { get; set; } = null!;
    public ICollection<Option> Options { get; set; } = [];
}
