using Microsoft.AspNetCore.Identity;

namespace LearnPath.API.Entities;

public class User : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<LearningPath> CreatedPaths { get; set; } = [];
    public ICollection<Progress> Progresses { get; set; } = [];
    public ICollection<UserClassroom> UserClassrooms { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}