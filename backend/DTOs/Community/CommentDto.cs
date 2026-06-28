namespace LearnPath.API.DTOs.Community;

public class CreateCommentDto
{
    public string Content { get; set; } = string.Empty;
    public int? ParentCommentId { get; set; }
}

public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int PostId { get; set; }
    public int? ParentCommentId { get; set; }
    public int UpvoteCount { get; set; }
    public int UserVote { get; set; }
    public List<CommentDto> Replies { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class VoteDto
{
    public bool IsUpvote { get; set; }
}
