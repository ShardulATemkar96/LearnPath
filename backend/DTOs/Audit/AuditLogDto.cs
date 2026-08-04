using LearnPath.API.Entities;

namespace LearnPath.API.DTOs.Audit;

public class AuditLogResponseDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public AuditAction ActionType { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AdditionalData { get; set; }
}

public class AuditLogListResponseDto
{
    public List<AuditLogResponseDto> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AuditLogQueryDto
{
    public AuditAction? ActionType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AuditActionOptionDto
{
    public AuditAction ActionType { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class AdminAuditLogResponseDto : AuditLogResponseDto
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }

    /// <summary>
    /// True/false when the referenced entity is navigable and its presence was
    /// resolved; null when the entity type has no destination to navigate to.
    /// </summary>
    public bool? EntityExists { get; set; }
}

public class AdminAuditLogListResponseDto
{
    public List<AdminAuditLogResponseDto> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminAuditLogQueryDto
{
    public string? UserId { get; set; }
    public string? Role { get; set; }
    public AuditAction? ActionType { get; set; }
    public string? EntityType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    /// <summary>"oldest" sorts ascending; anything else keeps newest first.</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AuditAccessDto
{
    public bool CanAccess { get; set; }
}
