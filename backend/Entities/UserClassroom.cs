namespace LearnPath.API.Entities;

public class UserClassroom
{
    public string UserId { get; set; } = string.Empty;
    public int ClassroomId { get; set; }
    public string Role { get; set; } = "Student"; // Student | Instructor
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
}