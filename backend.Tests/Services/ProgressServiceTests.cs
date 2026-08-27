using FluentAssertions;
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
}
