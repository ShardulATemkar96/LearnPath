using LearnPath.API.Data;
using LearnPath.API.DTOs.Admin;
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

    public AdminService(
        ApplicationDbContext context,
        UserManager<Entities.User> userManager)
    {
        _context     = context;
        _userManager = userManager;
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
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search));

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<AdminUserResponseDto>();
        foreach (var user in users)
            result.Add(await BuildUserDtoAsync(user));

        return result;
    }

    public async Task<AdminUserResponseDto> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");
        return await BuildUserDtoAsync(user);
    }

    public async Task<AdminUserResponseDto> UpdateUserRoleAsync(string userId, UpdateUserRoleDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var validRoles = new[] { "Admin", "Instructor", "Student" };
        if (!validRoles.Contains(dto.Role))
            throw new ArgumentException("Invalid role.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, dto.Role);

        return await BuildUserDtoAsync(user);
    }

    public async Task<AdminUserResponseDto> ToggleUserStatusAsync(string userId, ToggleUserStatusDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive  = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await BuildUserDtoAsync(user);
    }

    public async Task DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        await _userManager.DeleteAsync(user);
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

    private async Task<AdminUserResponseDto> BuildUserDtoAsync(Entities.User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var pathsCreated = await _context.LearningPaths.CountAsync(p => p.CreatedById == user.Id);

        var modulesCompleted = await _context.Progresses.CountAsync(p => p.UserId == user.Id && p.IsCompleted);

        return new AdminUserResponseDto
        {
            UserId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email!,
            IsActive = user.IsActive,
            Roles = roles,
            CreatedAt = user.CreatedAt,
            TotalPathsCreated = pathsCreated,
            TotalModulesCompleted = modulesCompleted,
        };
    }
}
