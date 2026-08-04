using LearnPath.API.Common;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Admin;
using LearnPath.API.DTOs.Certificate;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.DTOs.LearningPath;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public AdminController(
        IAdminService service,
        ApplicationDbContext context,
        IAuditLogService auditLog)
    {
        _service = service;
        _context = context;
        _auditLog = auditLog;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _service.GetStatsAsync();
        return Ok(ApiResponse<AdminStatsResponseDto>.Ok(result));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search)
    {
        var result = await _service.GetAllUsersAsync(search);
        return Ok(ApiResponse<List<AdminUserResponseDto>>.Ok(result));
    }

    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var result = await _service.GetUserByIdAsync(userId);
        return Ok(ApiResponse<AdminUserResponseDto>.Ok(result));
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateRole(string userId, [FromBody] UpdateUserRoleDto dto)
    {
        var result = await _service.UpdateUserRoleAsync(userId, dto, UserId);
        return Ok(ApiResponse<AdminUserResponseDto>.Ok(result, "Role updated."));
    }

    [HttpPut("users/{userId}/invalid")]
    public async Task<IActionResult> MarkInvalid(string userId, [FromBody] MarkInvalidDto dto)
    {
        try
        {
            var result = await _service.MarkInvalidAsync(userId, dto, UserId);
            return Ok(ApiResponse<AdminUserResponseDto>.Ok(result, "User marked invalid."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("users/{userId}/activate")]
    public async Task<IActionResult> ActivateUser(string userId)
    {
        try
        {
            var result = await _service.ActivateUserAsync(userId);
            return Ok(ApiResponse<AdminUserResponseDto>.Ok(result, "User activated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("users/{userId}/deactivate")]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        try
        {
            var result = await _service.DeactivateUserAsync(userId);
            return Ok(ApiResponse<AdminUserResponseDto>.Ok(result, "User deactivated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            await _service.DeleteUserAsync(userId);
            return Ok(ApiResponse<object>.Ok(null!, "User deleted."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("paths")]
    public async Task<IActionResult> GetAllPaths()
    {
        var result = await _service.GetAllPathsAsync();
        return Ok(ApiResponse<List<LearningPathResponseDto>>.Ok(result));
    }

    // ── Certificates ──────────────────────────────────────────

    [HttpGet("certificates")]
    public async Task<IActionResult> GetCertificates(
        [FromQuery] string? search,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Certificates
            .Include(c => c.User)
            .Include(c => c.LearningPath)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.User.FirstName.Contains(term)
                || c.User.LastName.Contains(term)
                || (c.User.FirstName + " " + c.User.LastName).Contains(term)
                || c.User.Email!.Contains(term)
                || c.LearningPath.Title.Contains(term));
        }

        if (DateTime.TryParse(fromDate, out var from))
            query = query.Where(c => c.IssuedAt >= from);

        if (DateTime.TryParse(toDate, out var to))
            query = query.Where(c => c.IssuedAt <= to);

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(c => c.IssuedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCertificateResponseDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.User.FirstName + " " + c.User.LastName,
                UserEmail = c.User.Email ?? string.Empty,
                LearningPathId = c.LearningPathId,
                LearningPathTitle = c.LearningPath.Title,
                CertificateNumber = $"CERT-{c.Id:D6}",
                CertificateUrl = c.CertificateUrl,
                IssuedAt = c.IssuedAt,
                CompletedAt = c.IssuedAt,
                Status = "Issued",
            })
            .ToListAsync();

        return Ok(ApiResponse<AdminCertificateListResponseDto>.Ok(new AdminCertificateListResponseDto
        {
            Entries = entries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("certificates/{id:int}")]
    public async Task<IActionResult> GetCertificate(int id)
    {
        var certificate = await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.LearningPath)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Certificate not found.");

        return Ok(ApiResponse<AdminCertificateResponseDto>.Ok(MapCertificate(certificate)));
    }

    [HttpDelete("certificates/{id:int}")]
    public async Task<IActionResult> DeleteCertificate(int id)
    {
        var certificate = await _context.Certificates
            .Include(c => c.LearningPath)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Certificate not found.");

        var pathTitle = certificate.LearningPath.Title;

        _context.Certificates.Remove(certificate);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CERTIFICATE_DELETED,
            "Certificate",
            certificate.Id.ToString(),
            $"Certificate {certificate.CertificateUrl} for learning path '{pathTitle}' deleted.");

        return Ok(ApiResponse<object>.Ok(null!, "Certificate deleted."));
    }

    private static AdminCertificateResponseDto MapCertificate(Certificate certificate)
    {
        return new AdminCertificateResponseDto
        {
            Id = certificate.Id,
            UserId = certificate.UserId,
            UserName = certificate.User.FirstName + " " + certificate.User.LastName,
            UserEmail = certificate.User.Email ?? string.Empty,
            LearningPathId = certificate.LearningPathId,
            LearningPathTitle = certificate.LearningPath.Title,
            CertificateNumber = $"CERT-{certificate.Id:D6}",
            CertificateUrl = certificate.CertificateUrl,
            IssuedAt = certificate.IssuedAt,
            CompletedAt = certificate.IssuedAt,
            Status = "Issued",
        };
    }

    // ── Classrooms ───────────────────────────────────────────

    [HttpGet("classrooms")]
    public async Task<IActionResult> GetClassrooms(
        [FromQuery] string? search,
        [FromQuery] int? learningPathId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Classrooms
            .Include(c => c.CreatedBy)
            .Include(c => c.LearningPath)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Title.Contains(term)
                || c.InviteCode.Contains(term)
                || c.LearningPath.Title.Contains(term)
                || (c.CreatedBy.FirstName + " " + c.CreatedBy.LastName).Contains(term)
                || c.CreatedBy.Email!.Contains(term));
        }

        if (learningPathId.HasValue)
            query = query.Where(c => c.LearningPathId == learningPathId.Value);

        // The system has no archived classrooms today; every classroom is
        // Active. The filter is honoured so archived state can be introduced
        // later without an API change.
        if (string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
            query = query.Where(c => false);

        var totalCount = await query.CountAsync();

        var entries = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminClassroomResponseDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                InviteCode = c.InviteCode,
                LearningPathId = c.LearningPathId,
                LearningPathTitle = c.LearningPath.Title,
                CreatedById = c.CreatedById,
                CreatedByName = c.CreatedBy.FirstName + " " + c.CreatedBy.LastName,
                CreatedByEmail = c.CreatedBy.Email ?? string.Empty,
                MemberCount = c.UserClassrooms.Count,
                Status = "Active",
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync();

        return Ok(ApiResponse<AdminClassroomListResponseDto>.Ok(new AdminClassroomListResponseDto
        {
            Entries = entries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        }));
    }

    [HttpGet("classrooms/{id:int}")]
    public async Task<IActionResult> GetClassroom(int id)
    {
        var classroom = await _context.Classrooms
            .Include(c => c.CreatedBy)
            .Include(c => c.LearningPath)
            .Include(c => c.UserClassrooms)
                .ThenInclude(uc => uc.User)
            .Include(c => c.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Classroom not found.");

        var dto = MapAdminClassroom(classroom);
        dto.AssignmentCount = classroom.Assignments.Count;
        dto.Members = classroom.UserClassrooms
            .OrderBy(uc => uc.JoinedAt)
            .Select(uc => new ClassroomMemberDto
            {
                UserId = uc.UserId,
                FullName = $"{uc.User.FirstName} {uc.User.LastName}",
                Email = uc.User.Email ?? string.Empty,
                Role = uc.Role,
                Status = uc.User.Status,
                InvalidReason = uc.User.InvalidReason,
                JoinedAt = uc.JoinedAt,
            })
            .ToList();

        return Ok(ApiResponse<AdminClassroomDetailResponseDto>.Ok(dto));
    }

    [HttpPut("classrooms/{id:int}")]
    public async Task<IActionResult> UpdateClassroom(int id, [FromBody] UpdateAdminClassroomDto dto)
    {
        var classroom = await _context.Classrooms
            .Include(c => c.CreatedBy)
            .Include(c => c.LearningPath)
            .Include(c => c.UserClassrooms)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Classroom not found.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Classroom title is required.");

        classroom.Title = dto.Title.Trim();
        classroom.Description = dto.Description?.Trim() ?? string.Empty;

        if (dto.LearningPathId.HasValue && dto.LearningPathId.Value != classroom.LearningPathId)
        {
            if (!await _context.LearningPaths.AnyAsync(p => p.Id == dto.LearningPathId.Value))
                throw new KeyNotFoundException("Learning path not found.");
            classroom.LearningPathId = dto.LearningPathId.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.TrainerId) && dto.TrainerId != classroom.CreatedById)
        {
            if (!await _context.Users.AnyAsync(u => u.Id == dto.TrainerId))
                throw new KeyNotFoundException("Trainer not found.");
            classroom.CreatedById = dto.TrainerId;
        }

        classroom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_UPDATED,
            "Classroom",
            classroom.Id.ToString(),
            $"Classroom '{classroom.Title}' was updated by an administrator.",
            additionalData: $"LearningPathId: {classroom.LearningPathId}; TrainerId: {classroom.CreatedById}");

        return Ok(ApiResponse<AdminClassroomResponseDto>.Ok(MapAdminClassroom(classroom)));
    }

    [HttpPut("classrooms/{id:int}/learning-path")]
    public async Task<IActionResult> ReassignLearningPath(int id, [FromBody] ReassignClassroomLearningPathDto dto)
    {
        var classroom = await _context.Classrooms
            .Include(c => c.LearningPath)
            .Include(c => c.UserClassrooms)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Classroom not found.");

        if (!await _context.LearningPaths.AnyAsync(p => p.Id == dto.LearningPathId))
            throw new KeyNotFoundException("Learning path not found.");

        if (classroom.LearningPathId == dto.LearningPathId)
            throw new ArgumentException("The classroom is already assigned to this learning path.");

        var oldPathTitle = classroom.LearningPath.Title;
        var newPathTitle = (await _context.LearningPaths
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == dto.LearningPathId))!.Title;

        classroom.LearningPathId = dto.LearningPathId;
        classroom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_UPDATED,
            "Classroom",
            classroom.Id.ToString(),
            $"Classroom '{classroom.Title}' was reassigned from learning path '{oldPathTitle}' to '{newPathTitle}' by an administrator.");

        return Ok(ApiResponse<AdminClassroomResponseDto>.Ok(MapAdminClassroom(classroom)));
    }

    [HttpDelete("classrooms/{id:int}")]
    public async Task<IActionResult> DeleteClassroom(int id)
    {
        var classroom = await _context.Classrooms
            .Include(c => c.LearningPath)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Classroom not found.");

        var title = classroom.Title;
        var pathTitle = classroom.LearningPath.Title;

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_DELETED,
            "Classroom",
            classroom.Id.ToString(),
            $"Classroom '{title}' for learning path '{pathTitle}' was deleted by an administrator.");

        return Ok(ApiResponse<object>.Ok(null!, "Classroom deleted."));
    }

    private static AdminClassroomDetailResponseDto MapAdminClassroom(Entities.Classroom c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Description = c.Description,
        InviteCode = c.InviteCode,
        LearningPathId = c.LearningPathId,
        LearningPathTitle = c.LearningPath?.Title ?? string.Empty,
        CreatedById = c.CreatedById,
        CreatedByName = c.CreatedBy is not null
            ? $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}" : string.Empty,
        CreatedByEmail = c.CreatedBy?.Email ?? string.Empty,
        MemberCount = c.UserClassrooms?.Count ?? 0,
        Status = "Active",
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
    };
}

