using FluentAssertions;
using LearnPath.API.Entities;
using LearnPath.API.Services.Analytics;
using LearnPath.Tests.Helpers;
using Xunit;

namespace LearnPath.Tests.Services;

public class AnalyticsServiceTests
{
    /* ── GetQuizAnalytics ──────────────────────────────── */

    [Fact]
    public async Task GetQuizAnalyticsAsync_NoAttempts_ReturnsZeroes()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1));
        await ctx.SaveChangesAsync();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetQuizAnalyticsAsync(1);

        result.TotalAttempts.Should().Be(0);
        result.AverageScore.Should().Be(0);
        result.PassPercentage.Should().Be(0);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_WithAttempts_ReturnsStats()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 2));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1, questionCount: 2));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 1, userId: userId, quizId: 1,
            status: AttemptStatus.Evaluated, score: 2, percentage: 100, passed: true));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 2, userId: userId + "b", quizId: 1,
            status: AttemptStatus.Evaluated, score: 0, percentage: 0, passed: false));
        await ctx.SaveChangesAsync();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetQuizAnalyticsAsync(1);

        result.TotalAttempts.Should().Be(2);
        result.UniqueStudents.Should().Be(2);
        result.AverageScore.Should().Be(50);
        result.PassPercentage.Should().Be(50);
        result.TotalPassed.Should().Be(1);
        result.TotalFailed.Should().Be(1);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_ScoreDistribution_CalculatesRanges()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1));
        var userId = Guid.NewGuid().ToString();
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 1, userId: userId, quizId: 1,
            status: AttemptStatus.Evaluated, score: 1, percentage: 10, passed: false));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 2, userId: userId + "2", quizId: 1,
            status: AttemptStatus.Evaluated, score: 2, percentage: 30, passed: false));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 3, userId: userId + "3", quizId: 1,
            status: AttemptStatus.Evaluated, score: 3, percentage: 50, passed: true));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 4, userId: userId + "4", quizId: 1,
            status: AttemptStatus.Evaluated, score: 4, percentage: 70, passed: true));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 5, userId: userId + "5", quizId: 1,
            status: AttemptStatus.Evaluated, score: 5, percentage: 90, passed: true));
        await ctx.SaveChangesAsync();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetQuizAnalyticsAsync(1);

        result.ScoreDistribution.Should().HaveCount(5);
        result.ScoreDistribution[0].Range.Should().Be("0-20%");
        result.ScoreDistribution[0].Count.Should().Be(1);
        result.ScoreDistribution[1].Count.Should().Be(1);
        result.ScoreDistribution[2].Count.Should().Be(1);
        result.ScoreDistribution[3].Count.Should().Be(1);
        result.ScoreDistribution[4].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_QuestionAnalytics_IncludesSuccessRate()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        var bank = EntityFactory.CreateQuestionBank(id: 1, questionCount: 2);
        ctx.QuestionBanks.Add(bank);
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1, questionCount: 2));
        var attempt = EntityFactory.CreateQuizAttempt(id: 1, userId: userId, quizId: 1,
            status: AttemptStatus.Evaluated, score: 1, percentage: 50, passed: true);
        ctx.QuizAttempts.Add(attempt);
        var questions = bank.Questions.ToList();
        ctx.StudentAnswers.Add(new StudentAnswer
        {
            QuizAttemptId = 1,
            QuestionId = questions[0].Id,
            OptionId = questions[0].Options.First(o => o.IsCorrect).Id,
        });
        ctx.StudentAnswers.Add(new StudentAnswer
        {
            QuizAttemptId = 1,
            QuestionId = questions[1].Id,
            OptionId = questions[1].Options.First(o => !o.IsCorrect).Id,
        });
        await ctx.SaveChangesAsync();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetQuizAnalyticsAsync(1);

        result.QuestionAnalytics.Should().HaveCount(2);
        result.QuestionAnalytics[0].TimesAnswered.Should().Be(1);
        result.QuestionAnalytics[0].TimesCorrect.Should().Be(1);
        result.QuestionAnalytics[0].SuccessRate.Should().Be(100);
    }

    [Fact]
    public async Task GetQuizAnalyticsAsync_MissingQuiz_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new AnalyticsService(ctx);

        var act = () => svc.GetQuizAnalyticsAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /* ── GetUserAnalytics ──────────────────────────────── */

    [Fact]
    public async Task GetUserAnalyticsAsync_NoActivity_ReturnsZeroes()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetUserAnalyticsAsync("user1");

        result.TotalModulesCompleted.Should().Be(0);
        result.TotalPathsEnrolled.Should().Be(0);
        result.TotalCertificates.Should().Be(0);
        result.ActiveClassrooms.Should().Be(0);
        result.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public async Task GetUserAnalyticsAsync_WithCompletions_ReturnsCounts()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        var path = EntityFactory.CreateLearningPath(id: 1, createdById: userId);
        ctx.LearningPaths.Add(path);
        var module = EntityFactory.CreateModule(id: 1, learningPathId: 1);
        ctx.Modules.Add(module);
        ctx.Progresses.Add(new Progress
        {
            UserId = userId,
            ModuleId = 1,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetUserAnalyticsAsync(userId);

        result.TotalModulesCompleted.Should().Be(1);
        result.TotalPathsEnrolled.Should().Be(1);
    }

    [Fact]
    public async Task GetUserAnalyticsAsync_Streak_CalculatesCorrectly()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        for (int i = 0; i < 3; i++)
        {
            ctx.Progresses.Add(new Progress
            {
                UserId = userId,
                ModuleId = 1,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow.Date.AddDays(-i),
            });
        }
        await ctx.SaveChangesAsync();
        var svc = new AnalyticsService(ctx);

        var result = await svc.GetUserAnalyticsAsync(userId);

        result.CurrentStreak.Should().Be(3);
    }
}
