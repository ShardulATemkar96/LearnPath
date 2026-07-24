namespace LearnPath.API.Entities;

public class ModuleQuiz
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int QuizId { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;

    public Module Module { get; set; } = null!;
    public Quiz Quiz { get; set; } = null!;
}
