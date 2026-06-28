namespace LearnPath.API.Entities;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Author { get; set; } = null!;
    public Post Post { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
    public ICollection<CommentVote> Votes { get; set; } = [];
}
