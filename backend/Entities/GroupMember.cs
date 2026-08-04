namespace LearnPath.API.Entities;

public class GroupMember
{
    public int GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
