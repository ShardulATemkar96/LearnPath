using LearnPath.API.Data;
using LearnPath.API.DTOs.Analytics;
using LearnPath.API.Entities;
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

    public async Task<QuizAnalyticsResponseDto> GetQuizAnalyticsAsync(int quizId)
    {
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == quizId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        var attempts = await _context.QuizAttempts
            .Where(a => a.QuizId == quizId && a.Status >= AttemptStatus.Submitted)
            .ToListAsync();

        var scoredAttempts = attempts.Where(a => a.Percentage.HasValue).ToList();
        var totalAttempts = scoredAttempts.Count;
        var uniqueStudents = attempts.Select(a => a.UserId).Distinct().Count();

        var avgScore = totalAttempts > 0
            ? (double)Math.Round(scoredAttempts.Average(a => a.Percentage!.Value), 1)
            : 0.0;

        var passed = scoredAttempts.Count(a => a.Passed == true);
        var failed = scoredAttempts.Count(a => a.Passed == false);
        var passPct = totalAttempts > 0
            ? Math.Round((double)passed / totalAttempts * 100, 1)
            : 0.0;

        var distribution = new List<ScoreDistributionDto>
        {
            new() { Range = "0-20%", Count = scoredAttempts.Count(a => a.Percentage <= 20) },
            new() { Range = "21-40%", Count = scoredAttempts.Count(a => a.Percentage > 20 && a.Percentage <= 40) },
            new() { Range = "41-60%", Count = scoredAttempts.Count(a => a.Percentage > 40 && a.Percentage <= 60) },
            new() { Range = "61-80%", Count = scoredAttempts.Count(a => a.Percentage > 60 && a.Percentage <= 80) },
            new() { Range = "81-100%", Count = scoredAttempts.Count(a => a.Percentage > 80) },
        };

        var questionStats = await GetQuestionAnalyticsAsync(quizId, quiz.QuestionBankId);
        var sorted = questionStats.OrderBy(q => q.SuccessRate).ToList();
        var mostIncorrect = sorted.FirstOrDefault();
        var hardest = sorted.FirstOrDefault();

        return new QuizAnalyticsResponseDto
        {
            QuizId = quizId,
            QuizTitle = quiz.Title,
            TotalAttempts = totalAttempts,
            UniqueStudents = uniqueStudents,
            AverageScore = avgScore,
            PassPercentage = passPct,
            TotalPassed = passed,
            TotalFailed = failed,
            ScoreDistribution = distribution,
            QuestionAnalytics = questionStats,
            MostIncorrectQuestion = mostIncorrect,
            HardestQuestion = hardest,
        };
    }

    private async Task<List<QuestionAnalyticsDto>> GetQuestionAnalyticsAsync(int quizId, int questionBankId)
    {
        var questions = await _context.Questions
            .Where(q => q.QuestionBankId == questionBankId)
            .ToListAsync();

        var answers = await _context.StudentAnswers
            .Where(sa => sa.Attempt.QuizId == quizId && sa.Attempt.Status >= AttemptStatus.Submitted)
            .Include(sa => sa.Option)
            .ToListAsync();

        return questions.Select(q =>
        {
            var qAnswers = answers.Where(a => a.QuestionId == q.Id).ToList();
            var timesAnswered = qAnswers.Count;
            var timesCorrect = qAnswers.Count(a => a.Option.IsCorrect);

            return new QuestionAnalyticsDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText.Length > 100
                    ? q.QuestionText[..100] + "..."
                    : q.QuestionText,
                TimesAnswered = timesAnswered,
                TimesCorrect = timesCorrect,
                SuccessRate = timesAnswered > 0
                    ? Math.Round((double)timesCorrect / timesAnswered * 100, 1)
                    : 0.0,
                Difficulty = q.Difficulty.ToString(),
            };
        }).ToList();
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

        // ── Path completions ──────────────────────────────────                                                                                                                   Romishfroze
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