using LearnPath.API.Data;
using LearnPath.API.DTOs.Admin;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.DTOs.LearningPath;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Admin;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Entities.User>    _userManager;
    private readonly IAuditLogService _auditLog;

    public AdminService(
        ApplicationDbContext context,
        UserManager<Entities.User> userManager,
        IAuditLogService auditLog)
    {
        _context     = context;
        _userManager = userManager;
        _auditLog    = auditLog;
    }

    public async Task<AdminStatsResponseDto> GetStatsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalPaths = await _context.LearningPaths.CountAsync();
        var totalClassrooms = await _context.Classrooms.CountAsync();
        var totalCerts = await _context.Certificates.CountAsync();
        var totalCompleted = await _context.Progresses.CountAsync(p => p.IsCompleted);

        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var newThisMonth = await _context.Users
            .CountAsync(u => u.CreatedAt >= monthStart);

        var growth = new List<AdminUserGrowthDto>();
        for (int i = 5; i >= 0; i--)
        {
            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
            var end = start.AddMonths(1);

            var count = await _context.Users.CountAsync(u => u.CreatedAt >= start && u.CreatedAt < end);

            growth.Add(new AdminUserGrowthDto
            {
                Month = start.ToString("MMM"),
                Count = count,
            });
        }

        return new AdminStatsResponseDto
        {
            TotalUsers              = totalUsers,
            TotalLearningPaths      = totalPaths,
            TotalClassrooms         = totalClassrooms,
            TotalCertificatesIssued = totalCerts,
            TotalModulesCompleted   = totalCompleted,
            NewUsersThisMonth       = newThisMonth,
            UserGrowth              = growth,
        };
    }

    public async Task<List<AdminUserResponseDto>> GetAllUsersAsync(string? search)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.Email!.Contains(search) ||
                u.UserName!.Contains(search) ||
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var lastLogins = await _context.RefreshTokens
            .Where(r => userIds.Contains(r.UserId))
            .GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(r => r.CreatedAt) })
            .ToDictionaryAsync(x => x.UserId, x => x.Last);

        var result = new List<AdminUserResponseDto>();
        foreach (var user in users)
            result.Add(await BuildUserDtoAsync(user, lastLogins.GetValueOrDefault(user.Id)));

        return result;
    }

    public async Task<AdminUserResponseDto> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var lastLogin = await _context.RefreshTokens
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefaultAsync();

        return await BuildUserDtoAsync(user, lastLogin);
    }

    public async Task<AdminUserResponseDto> UpdateUserRoleAsync(string userId, UpdateUserRoleDto dto, string actorId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var actor = await _userManager.FindByIdAsync(actorId)
            ?? throw new UnauthorizedAccessException("Actor not found.");

        if (!actor.IsSuperAdmin)
            throw new UnauthorizedAccessException("Only the Super Admin can change user roles.");

        if (user.IsSuperAdmin)
            throw new ArgumentException("The Super Admin account cannot be reassigned.");

        var validRoles = new[] { "Admin", "Instructor", "Student" };
        if (!validRoles.Contains(dto.Role))
            throw new ArgumentException("Invalid role.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault();

        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, dto.Role);

        await _auditLog.LogAsync(
            AuditAction.ROLE_CHANGED,
            "User",
            user.Id,
            $"Role changed for '{user.Email}' from {oldRole ?? "none"} to {dto.Role}.",
            oldValue: oldRole,
            newValue: dto.Role);

        if (dto.Role == "Instructor")
            await _auditLog.LogAsync(
                AuditAction.INSTRUCTOR_ASSIGNED,
                "User",
                user.Id,
                $"'{user.Email}' was assigned the Instructor role.",
                newValue: dto.Role);

        if (oldRole == "Instructor" && dto.Role != "Instructor")
            await _auditLog.LogAsync(
                AuditAction.INSTRUCTOR_REMOVED,
                "User",
                user.Id,
                $"The Instructor role was removed from '{user.Email}'.",
                oldValue: oldRole,
                newValue: dto.Role);

        return await BuildUserDtoAsync(user);
    }

    public async Task<AdminUserResponseDto> ActivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Status == UserStatus.Deleted)
            throw new ArgumentException("Cannot activate a deleted user.");

        var wasInvalid = user.Status == UserStatus.Invalid;

        user.Status        = UserStatus.Active;
        user.InvalidReason = null;
        user.UpdatedAt     = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAsync(
            wasInvalid ? AuditAction.INVALID_REMOVED : AuditAction.USER_ACTIVATED,
            "User",
            user.Id,
            wasInvalid
                ? $"User '{user.Email}' was restored from Invalid status."
                : $"User '{user.Email}' was activated.",
            oldValue: wasInvalid ? "Invalid" : null,
            newValue: "Active");

        return await GetUserByIdAsync(userId);
    }

    public async Task<AdminUserResponseDto> DeactivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Status == UserStatus.Deleted)
            throw new ArgumentException("Cannot deactivate a deleted user.");

        user.Status        = UserStatus.Inactive;
        user.InvalidReason = null;
        user.UpdatedAt     = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAsync(
            AuditAction.USER_DEACTIVATED,
            "User",
            user.Id,
            $"User '{user.Email}' was deactivated.",
            oldValue: "Active",
            newValue: "Inactive");

        return await GetUserByIdAsync(userId);
    }

    public async Task<AdminUserResponseDto> MarkInvalidAsync(string userId, MarkInvalidDto dto, string actorId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var actor = await _userManager.FindByIdAsync(actorId)
            ?? throw new UnauthorizedAccessException("Actor not found.");

        if (!actor.IsSuperAdmin && !(await _userManager.IsInRoleAsync(actor, "Instructor")))
            throw new UnauthorizedAccessException("Only the Super Admin or an Instructor can mark users invalid.");

        if (user.IsSuperAdmin)
            throw new ArgumentException("The Super Admin account cannot be marked invalid.");

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("A reason is required.");

        if (user.Status == UserStatus.Deleted)
            throw new ArgumentException("Cannot mark a deleted user invalid.");

        user.Status        = UserStatus.Invalid;
        user.InvalidReason = dto.Reason.Trim();
        user.UpdatedAt     = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditLog.LogAsync(
            AuditAction.USER_MARKED_INVALID,
            "User",
            user.Id,
            $"User '{user.Email}' was marked invalid.",
            oldValue: "Active",
            newValue: "Invalid",
            additionalData: $"Reason: {dto.Reason.Trim()}");

        return await GetUserByIdAsync(userId);
    }

    public async Task DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var targetEmail = user.Email;

        if (user.IsSuperAdmin)
            throw new ArgumentException("The Super Admin account cannot be deleted.");

        if (user.Status == UserStatus.Deleted)
            throw new ArgumentException("User is already deleted.");

        var setUserName = await _userManager.SetUserNameAsync(user, $"deleted_user_{user.Id}");
        if (!setUserName.Succeeded)
            throw new ArgumentException(string.Join(" | ", setUserName.Errors.Select(e => e.Description)));

        var setEmail = await _userManager.SetEmailAsync(user, $"deleted_{user.Id}@deleted.local");
        if (!setEmail.Succeeded)
            throw new ArgumentException(string.Join(" | ", setEmail.Errors.Select(e => e.Description)));

        user.FirstName     = "Deleted";
        user.LastName      = "User";
        user.Bio           = "Deleted User";
        user.AvatarUrl     = null;
        user.EmailConfirmed = false;
        user.Status        = UserStatus.Deleted;
        user.UpdatedAt     = DateTime.UtcNow;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new ArgumentException(string.Join(" | ", update.Errors.Select(e => e.Description)));

        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();
        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.USER_SOFT_DELETED,
            "User",
            user.Id,
            $"User '{targetEmail}' was soft-deleted.",
            oldValue: "Active",
            newValue: "Deleted");
    }

    public async Task<List<LearningPathResponseDto>> GetAllPathsAsync()
    {
        return await _context.LearningPaths
            .Include(p => p.CreatedBy)
            .Include(p => p.Modules)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new LearningPathResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                ThumbnailUrl = p.ThumbnailUrl,
                IsPublished = p.IsPublished,
                IsPublic = p.IsPublic,
                CreatedById = p.CreatedById,
                CreatedByName = p.CreatedBy != null
                    ? p.CreatedBy.FirstName + " " + p.CreatedBy.LastName
                    : "",
                TotalModules = p.Modules.Count,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
            })
            .ToListAsync();
    }

    private async Task<AdminUserResponseDto> BuildUserDtoAsync(
        Entities.User user, DateTime? lastLoginAt = null)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var pathsCreated = await _context.LearningPaths.CountAsync(p => p.CreatedById == user.Id);

        var modulesCompleted = await _context.Progresses.CountAsync(p => p.UserId == user.Id && p.IsCompleted);

        var quizAttempts = await _context.QuizAttempts.CountAsync(a => a.UserId == user.Id);

        var certificates = await _context.Certificates.CountAsync(c => c.UserId == user.Id);

        var classroomsJoined = await _context.UserClassrooms.CountAsync(uc => uc.UserId == user.Id);

        return new AdminUserResponseDto
        {
            UserId                = user.Id,
            FullName              = $"{user.FirstName} {user.LastName}",
            UserName              = user.UserName ?? string.Empty,
            Email                 = user.Email ?? string.Empty,
            AvatarUrl             = user.AvatarUrl,
            PhoneNumber           = user.PhoneNumber,
            Bio                   = user.Bio,
            Status                = user.Status,
            InvalidReason         = user.InvalidReason,
            IsSuperAdmin          = user.IsSuperAdmin,
            Roles                 = roles,
            CreatedAt             = user.CreatedAt,
            LastLoginAt           = lastLoginAt,
            TotalPathsCreated     = pathsCreated,
            TotalModulesCompleted = modulesCompleted,
            TotalQuizAttempts     = quizAttempts,
            TotalCertificates     = certificates,
            TotalClassroomsJoined = classroomsJoined,
        };
    }
}
