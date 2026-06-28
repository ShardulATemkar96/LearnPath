namespace LearnPath.API.Entities;

public class CommentVote
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CommentId { get; set; }
    public bool IsUpvote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Comment Comment { get; set; } = null!;
}
