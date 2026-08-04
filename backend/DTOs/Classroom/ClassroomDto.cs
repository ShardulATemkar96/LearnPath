using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.Classroom;

public class CreateClassroomDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
}

public class UpdateClassroomDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ClassroomResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
    public string LearningPathTitle { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ClassroomDetailResponseDto : ClassroomResponseDto
{
    public List<ClassroomMemberDto> Members { get; set; } = [];
    public List<AssignmentResponseDto> Assignments { get; set; } = [];
}

public class ClassroomMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public string? InvalidReason { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class MarkInvalidDto
{
    public string Reason { get; set; } = string.Empty;
}
public class JoinClassroomDto
{
    public string InviteCode { get; set; } = string.Empty;
}

public class AdminClassroomResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InviteCode { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
    public string LearningPathTitle { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminClassroomListResponseDto
{
    public List<AdminClassroomResponseDto> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminClassroomDetailResponseDto : AdminClassroomResponseDto
{
    public List<ClassroomMemberDto> Members { get; set; } = [];
    public int AssignmentCount { get; set; }
}

public class UpdateAdminClassroomDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? LearningPathId { get; set; }
    public string? TrainerId { get; set; }
}

public class ReassignClassroomLearningPathDto
{
    public int LearningPathId { get; set; }
}
