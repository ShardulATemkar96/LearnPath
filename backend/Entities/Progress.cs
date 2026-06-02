namespace LearnPath.API.Entities;

public class Progress
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ModuleId { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Module Module { get; set; } = null!;
}