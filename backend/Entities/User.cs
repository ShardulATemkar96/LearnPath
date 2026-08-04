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
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsSuperAdmin { get; set; }
    public string? InvalidReason { get; set; }

    public ICollection<LearningPath> CreatedPaths { get; set; } = [];
    public ICollection<Progress> Progresses { get; set; } = [];
    public ICollection<UserClassroom> UserClassrooms { get; set; } = [];
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}