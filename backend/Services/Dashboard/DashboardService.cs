using LearnPath.API.Data;
using LearnPath.API.DTOs.Dashboard;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponseDto> GetDashboardDataAsync(string userId)
    {
        var enrolledPathIds = await _context.Progresses
            .Where(p => p.UserId == userId)
            .Select(p => p.Module.LearningPathId)
            .Distinct()
            .ToListAsync();

        var completedModules = await _context.Progresses
            .Where(p => p.UserId == userId && p.IsCompleted)
            .CountAsync();

        var activeClassrooms = await _context.UserClassrooms
            .Where(uc => uc.UserId == userId)
            .CountAsync();

        var certificates = await _context.Certificates
            .Where(c => c.UserId == userId)
            .CountAsync();

        var pathProgress = await _context.LearningPaths
            .Where(lp => enrolledPathIds.Contains(lp.Id))
            .Select(lp => new PathProgressDto
            {
                Id = lp.Id,
                Title = lp.Title,
                TotalModules = lp.Modules.Count,
                CompletedModules = lp.Modules
                    .Count(m => m.Progresses.Any(p => p.UserId == userId && p.IsCompleted)),
            })
            .ToListAsync();

        var recentActivity = await _context.Progresses
            .Where(p => p.UserId == userId && p.IsCompleted && p.CompletedAt != null)
            .OrderByDescending(p => p.CompletedAt)
            .Take(10)
            .Select(p => new ActivityItemDto
            {
                Id = p.Id,
                Type = "completion",
                Message = $"Completed module: {p.Module.Title}",
                Time = p.CompletedAt!.Value.ToString("MMM dd, yyyy"),
            })
            .ToListAsync();

        return new DashboardResponseDto
        {
            Stats = new DashboardStatsDto
            {
                EnrolledPaths = enrolledPathIds.Count,
                CompletedModules = completedModules,
                ActiveClassrooms = activeClassrooms,
                Certificates = certificates,
            },
            PathProgress = pathProgress,
            RecentActivity = recentActivity,
        };
    }
}
