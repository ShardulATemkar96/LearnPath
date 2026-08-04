namespace LearnPath.API.DTOs.Admin;

using LearnPath.API.Entities;

public class AdminUserResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public UserStatus Status { get; set; }
    public string? InvalidReason { get; set; }
    public bool IsSuperAdmin { get; set; }
    public IList<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalPathsCreated { get; set; }
    public int TotalModulesCompleted { get; set; }
    public int TotalQuizAttempts { get; set; }
    public int TotalCertificates { get; set; }
    public int TotalClassroomsJoined { get; set; }
}

public class AdminStatsResponseDto
{
    public int TotalUsers { get; set; }
    public int TotalLearningPaths { get; set; }
    public int TotalClassrooms { get; set; }
    public int TotalCertificatesIssued { get; set; }
    public int TotalModulesCompleted { get; set; }
    public int NewUsersThisMonth { get; set; }
    public List<AdminUserGrowthDto> UserGrowth { get; set; } = [];
}

public class AdminUserGrowthDto
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UpdateUserRoleDto
{
    public string Role { get; set; } = string.Empty;
}
