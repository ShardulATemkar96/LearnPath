using LearnPath.API.Data;
using LearnPath.API.DTOs.Analytics;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;

    public AnalyticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserAnalyticsResponseDto> GetUserAnalyticsAsync(string userId)
    {
        // ── Core counts ───────────────────────────────────────
        var completedModules = await _context.Progresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted);

        var enrolledPaths = await _context.Progresses
            .Where(p => p.UserId == userId)
            .Select(p => p.Module.LearningPathId)
            .Distinct()
            .CountAsync();

        var certificates = await _context.Certificates
            .CountAsync(c => c.UserId == userId);

        var activeClassrooms = await _context.UserClassrooms
            .CountAsync(uc => uc.UserId == userId);

        // ── Weekly activity (last 7 days) ─────────────────────
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-6).Date;
        var recentProgress = await _context.Progresses
            .Where(p => p.UserId == userId &&
                        p.IsCompleted &&
                        p.CompletedAt >= sevenDaysAgo)
            .Select(p => p.CompletedAt!.Value.Date)
            .ToListAsync();

        var weeklyActivity = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = sevenDaysAgo.AddDays(offset);
            var count = recentProgress.Count(d => d == day);
            return new WeeklyActivityDto
            {
                Day = day.ToString("ddd"),
                ModulesCompleted = count,
            };
        }).ToList();

        // ── Streak ────────────────────────────────────────────
        var streak = CalculateStreak(recentProgress);

        // ── Path completions ──────────────────────────────────
        var enrolledPathIds = await _context.Progresses
            .Where(p => p.UserId == userId)
            .Select(p => p.Module.LearningPathId)
            .Distinct()
            .ToListAsync();

        var pathCompletions = new List<PathCompletionDto>();
        foreach (var pathId in enrolledPathIds)
        {
            var path = await _context.LearningPaths
                .Include(p => p.Modules)
                .FirstOrDefaultAsync(p => p.Id == pathId);
            if (path is null) continue;

            var completed = await _context.Progresses
                .CountAsync(p => p.UserId == userId && p.IsCompleted &&
                                 path.Modules.Select(m => m.Id).Contains(p.ModuleId));
            var total = path.Modules.Count;

            pathCompletions.Add(new PathCompletionDto
            {
                Title = path.Title,
                CompletedModules = completed,
                TotalModules = total,
                Percent = total > 0 ? (int)Math.Round((double)completed / total * 100) : 0,
            });
        }

        // ── Module type breakdown ─────────────────────────────
        var breakdown = await _context.Progresses
            .Where(p => p.UserId == userId && p.IsCompleted)
            .GroupBy(p => p.Module.ContentType)
            .Select(g => new ModuleTypeBreakdownDto
            {
                ContentType = g.Key,
                Count = g.Count(),
            })
            .ToListAsync();

        // ── Average completion rate ───────────────────────────
        var avgRate = pathCompletions.Any()
            ? Math.Round(pathCompletions.Average(p => p.Percent), 1)
            : 0;

        return new UserAnalyticsResponseDto
        {
            TotalModulesCompleted = completedModules,
            TotalPathsEnrolled = enrolledPaths,
            TotalCertificates = certificates,
            ActiveClassrooms = activeClassrooms,
            CurrentStreak = streak,
            AverageCompletionRate = avgRate,
            WeeklyActivity = weeklyActivity,
            PathCompletions = pathCompletions,
            ModuleTypeBreakdown = breakdown,
        };
    }

    private static int CalculateStreak(List<DateTime> completedDates)
    {
        if (!completedDates.Any()) return 0;
        var streak = 0;
        var current = DateTime.UtcNow.Date;
        var dateSet = completedDates.Select(d => d.Date).ToHashSet();

        while (dateSet.Contains(current))
        {
            streak++;
            current = current.AddDays(-1);
        }
        return streak;
    }
}