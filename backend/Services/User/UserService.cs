using LearnPath.API.Data;
using LearnPath.API.DTOs.User;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.User;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Entities.User> _userManager;
    private readonly IAuditLogService _auditLog;

    public UserService(
        ApplicationDbContext context,
        UserManager<Entities.User> userManager,
        IAuditLogService auditLog)
    {
        _context     = context;
        _userManager = userManager;
        _auditLog    = auditLog;
    }

    public async Task<UserProfileResponseDto> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        var totalPaths = await _context.LearningPaths
            .CountAsync(p => p.CreatedById == userId);

        var totalCompleted = await _context.Progresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted);

        var totalCerts = await _context.Certificates
            .CountAsync(c => c.UserId == userId);

        return new UserProfileResponseDto
        {
            UserId                = user.Id,
            FirstName             = user.FirstName,
            LastName              = user.LastName,
            Email                 = user.Email!,
            AvatarUrl             = user.AvatarUrl,
            Bio                   = user.Bio,
            Roles                 = roles,
            CreatedAt             = user.CreatedAt,
            TotalPathsCreated     = totalPaths,
            TotalModulesCompleted = totalCompleted,
            TotalCertificates     = totalCerts,
        };
    }

    public async Task<UserProfileResponseDto> UpdateProfileAsync(
        string userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.FirstName  = dto.FirstName;
        user.LastName   = dto.LastName;
        user.AvatarUrl  = dto.AvatarUrl;
        user.Bio        = dto.Bio;
        user.UpdatedAt  = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        await _auditLog.LogAsync(
            AuditAction.PROFILE_UPDATED,
            "User",
            user.Id,
            $"User '{user.Email}' updated their profile.",
            userId: user.Id,
            username: user.Email,
            role: roles.FirstOrDefault());

        return await GetProfileAsync(userId);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var result = await _userManager.ChangePasswordAsync(
            user, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
            throw new ArgumentException(
                string.Join(" | ", result.Errors.Select(e => e.Description)));

        var roles = await _userManager.GetRolesAsync(user);
        await _auditLog.LogAsync(
            AuditAction.PASSWORD_CHANGED,
            "User",
            user.Id,
            $"User '{user.Email}' changed their password.",
            userId: user.Id,
            username: user.Email,
            role: roles.FirstOrDefault());
    }
}
