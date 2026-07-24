namespace LearnPath.API.Entities;

public class QuestionBank
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int Version { get; set; } = 1;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredJson { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public QuestionBankStatus Status { get; set; } = QuestionBankStatus.Draft;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ArchivedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<Quiz> Quizzes { get; set; } = [];
}
