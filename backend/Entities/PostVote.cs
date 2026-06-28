namespace LearnPath.API.Entities;

public class PostVote
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PostId { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
