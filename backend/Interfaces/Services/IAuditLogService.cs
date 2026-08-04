using LearnPath.API.DTOs.Audit;
using LearnPath.API.Entities;

namespace LearnPath.API.Interfaces.Services;

public interface IAuditLogService
{
    /// <summary>
    /// Returns the caller's own immutable history, newest first.
    /// Never returns entries belonging to another user.
    /// </summary>
    Task<AuditLogListResponseDto> GetMyHistoryAsync(string userId, AuditLogQueryDto query);

    List<AuditActionOptionDto> GetActionOptions();

    /// <summary>
    /// Super Admin only view over every user's history.
    /// Throws <see cref="UnauthorizedAccessException"/> (403) for anyone else.
    /// </summary>
    Task<AdminAuditLogListResponseDto> GetAdminHistoryAsync(
        string requestingUserId, AdminAuditLogQueryDto query);

    /// <summary>Reports whether the caller may use the Super Admin history view.</summary>
    Task<bool> CanAccessAdminHistoryAsync(string requestingUserId);

    /// <summary>Distinct entity types present in the audit trail, for filtering.</summary>
    Task<List<string>> GetEntityTypesAsync(string requestingUserId);

    Task LogAsync(
        AuditAction actionType,
        string entityType,
        string entityId,
        string description,
        string? oldValue = null,
        string? newValue = null,
        string? additionalData = null,
        string? userId = null,
        string? username = null,
        string? role = null);
}
