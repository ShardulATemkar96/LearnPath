namespace LearnPath.API.DTOs.Community;

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}

public class UpdateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}

public class GroupMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public int MemberCount { get; set; }
    public int PostCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GroupDetailDto : GroupDto
{
    public List<GroupMemberDto> Members { get; set; } = [];
    public string? CurrentUserRole { get; set; }
}

public class GroupListResponseDto
{
    public List<GroupDto> Groups { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
