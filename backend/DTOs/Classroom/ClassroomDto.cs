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
    public DateTime JoinedAt { get; set; }
}
