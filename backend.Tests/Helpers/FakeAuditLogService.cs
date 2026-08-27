using LearnPath.API.DTOs.Audit;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;

namespace LearnPath.Tests.Helpers;

public class FakeAuditLogService : IAuditLogService
{
    public Task<AuditLogListResponseDto> GetMyHistoryAsync(string userId, AuditLogQueryDto query) => Task.FromResult(new AuditLogListResponseDto());
    public List<AuditActionOptionDto> GetActionOptions() => [];
    public Task<AdminAuditLogListResponseDto> GetAdminHistoryAsync(string requestingUserId, AdminAuditLogQueryDto query) => Task.FromResult(new AdminAuditLogListResponseDto());
    public Task<bool> CanAccessAdminHistoryAsync(string requestingUserId) => Task.FromResult(true);
    public Task<List<string>> GetEntityTypesAsync(string requestingUserId) => Task.FromResult(new List<string>());
    public Task LogAsync(AuditAction actionType, string entityType, string entityId, string description, string? oldValue = null, string? newValue = null, string? additionalData = null, string? userId = null, string? username = null, string? role = null) => Task.CompletedTask;
}
