namespace LearnPath.API.Entities;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public string? ProgrammingLanguage { get; set; }
    public string? Tags { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public int? LearningPathId { get; set; }
    public int? GroupId { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsPinned { get; set; } = false;
    public bool IsLocked { get; set; } = false;
    public int ViewCount { get; set; } = 0;
    public int Score { get; set; } = 0;
    public bool Edited { get; set; } = false;
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Author { get; set; } = null!;
    public LearningPath? LearningPath { get; set; }
    public Group? Group { get; set; }
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<PostVote> Votes { get; set; } = [];
}
