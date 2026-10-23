using FluentAssertions;
using LearnPath.API.DTOs.Progress;
using LearnPath.API.Entities;
using LearnPath.API.Services.Progress;
using LearnPath.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnPath.Tests.Services;

public class ProgressServiceTests
{
    [Fact]
    public async Task MarkModuleCompleteFromQuizAsync_CreatesCompletedProgress()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1));
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        await svc.MarkModuleCompleteFromQuizAsync(userId, 1);

        var progress = await ctx.Progresses.FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == 1);
        progress.Should().NotBeNull();
        progress!.IsCompleted.Should().BeTrue();
        progress.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkModuleCompleteFromQuizAsync_AlreadyCompleted_IsIdempotent()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1));
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        await svc.MarkModuleCompleteFromQuizAsync(userId, 1);
        var act = () => svc.MarkModuleCompleteFromQuizAsync(userId, 1);

        await act.Should().NotThrowAsync();
        (await ctx.Progresses.CountAsync(p => p.UserId == userId && p.ModuleId == 1)).Should().Be(1);
    }

    [Fact]
    public async Task MarkModuleCompleteFromQuizAsync_MissingModule_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        var act = () => svc.MarkModuleCompleteFromQuizAsync(Guid.NewGuid().ToString(), 999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /* ── Unlock reconciliation (ModuleUnlockRules) ───────── */

    [Fact]
    public async Task MarkComplete_SequentialModuleBlockedUntilPreviousComplete()
    {
        var userId = Guid.NewGuid().ToString();
        var ctx = DbContextFactory.Create();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, order: 2, title: "M2"));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 3, learningPathId: 1, order: 3, title: "M3"));
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        var act = () => svc.MarkCompleteAsync(new MarkCompleteDto { ModuleId = 2 }, userId);

        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("Complete the previous module to unlock this module.");
        (await ctx.Progresses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task MarkComplete_AfterAllPrerequisitesComplete_Succeeds()
    {
        var userId = Guid.NewGuid().ToString();
        var ctx = DbContextFactory.Create();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, order: 2));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 3, learningPathId: 1, order: 3, prereqIds: [1, 2]));
        ctx.Progresses.Add(new Progress { Id = 1, UserId = userId, ModuleId = 1, IsCompleted = true });
        ctx.Progresses.Add(new Progress { Id = 2, UserId = userId, ModuleId = 2, IsCompleted = true });
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        var result = await svc.MarkCompleteAsync(new MarkCompleteDto { ModuleId = 3 }, userId);

        result.IsCompleted.Should().BeTrue();
        result.ModuleId.Should().Be(3);
    }

    [Fact]
    public async Task MarkComplete_MissingPrerequisite_ThrowsAndPersistsNothing()
    {
        var userId = Guid.NewGuid().ToString();
        var ctx = DbContextFactory.Create();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, order: 2, prereqIds: [1]));
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        var act = () => svc.MarkCompleteAsync(new MarkCompleteDto { ModuleId = 2 }, userId);

        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("Complete all prerequisite modules first.");
        (await ctx.Progresses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GetPathProgress_SummaryMatchesDetailUnlockRules()
    {
        var userId = Guid.NewGuid().ToString();
        var ctx = DbContextFactory.Create();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, order: 2, title: "M2"));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 3, learningPathId: 1, order: 3, title: "M3", prereqIds: [1]));
        ctx.Progresses.Add(new Progress { Id = 1, UserId = userId, ModuleId = 1, IsCompleted = true });
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        var summary = await svc.GetPathProgressAsync(1, userId);

        var byId = summary.Modules.ToDictionary(m => m.ModuleId);
        byId[1].IsUnlocked.Should().BeTrue();  // first module is free
        byId[1].IsCompleted.Should().BeTrue();
        byId[2].IsUnlocked.Should().BeTrue();  // sequential: predecessor complete
        byId[3].IsUnlocked.Should().BeTrue();  // DAG: prerequisite 1 complete
        summary.CompletedModules.Should().Be(1);
        summary.ProgressPercent.Should().Be(33);
    }

    [Fact]
    public async Task GetPathProgress_LockedModules_ReportLocked()
    {
        var userId = Guid.NewGuid().ToString();
        var ctx = DbContextFactory.Create();
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, order: 2, title: "M2"));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 3, learningPathId: 1, order: 3, title: "M3", prereqIds: [1]));
        await ctx.SaveChangesAsync();
        var svc = new ProgressService(ctx, new FakeAuditLogService());

        var summary = await svc.GetPathProgressAsync(1, userId);

        var byId = summary.Modules.ToDictionary(m => m.ModuleId);
        byId[1].IsUnlocked.Should().BeTrue();
        byId[2].IsUnlocked.Should().BeFalse(); // sequential predecessor not completed
        byId[3].IsUnlocked.Should().BeFalse(); // DAG prerequisite not completed
    }
}
