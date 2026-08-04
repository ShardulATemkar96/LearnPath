using System.Security.Claims;
using LearnPath.API.Common;
using LearnPath.API.DTOs.Audit;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearnPath.API.Controllers;

/// <summary>
/// Read-only access to the caller's own immutable activity history.
/// There is deliberately no route that accepts a user id, and no
/// write, update or delete endpoint: audit history is append-only.
/// </summary>
[ApiController]
[Route("api/v1/history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly IAuditLogService _auditLog;

    public HistoryController(IAuditLogService auditLog) => _auditLog = auditLog;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("me")]
    public async Task<IActionResult> GetMyHistory([FromQuery] AuditLogQueryDto query)
    {
        var result = await _auditLog.GetMyHistoryAsync(UserId, query);
        return Ok(ApiResponse<AuditLogListResponseDto>.Ok(result));
    }

    [HttpGet("action-types")]
    public IActionResult GetActionTypes()
    {
        var result = _auditLog.GetActionOptions();
        return Ok(ApiResponse<List<AuditActionOptionDto>>.Ok(result));
    }

    // ── Super Admin history ───────────────────────────────────
    // Still read-only: these expose other users' history to the Super Admin
    // alone. The service re-checks the Super Admin flag on every call, so the
    // Admin role attribute below is a first gate, never the only one.

    [HttpGet("admin/access")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminAccess()
    {
        var canAccess = await _auditLog.CanAccessAdminHistoryAsync(UserId);
        return Ok(ApiResponse<AuditAccessDto>.Ok(new AuditAccessDto { CanAccess = canAccess }));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminHistory([FromQuery] AdminAuditLogQueryDto query)
    {
        var result = await _auditLog.GetAdminHistoryAsync(UserId, query);
        return Ok(ApiResponse<AdminAuditLogListResponseDto>.Ok(result));
    }

    [HttpGet("admin/entity-types")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEntityTypes()
    {
        var result = await _auditLog.GetEntityTypesAsync(UserId);
        return Ok(ApiResponse<List<string>>.Ok(result));
    }
}
