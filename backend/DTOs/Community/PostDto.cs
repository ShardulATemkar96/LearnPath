namespace LearnPath.API.DTOs.Community;

public class CreatePostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int? LearningPathId { get; set; }
}

public class UpdatePostDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class PostSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsLocked { get; set; }
    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public int CommentCount { get; set; }
    public int UserVote { get; set; }
    public string? LearningPathTitle { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PostDetailDto : PostSummaryDto
{
    public string Content { get; set; } = string.Empty;
    public List<CommentDto> Comments { get; set; } = [];
}

public class PostListResponseDto
{
    public List<PostSummaryDto> Posts { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
