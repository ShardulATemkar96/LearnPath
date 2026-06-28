using FluentAssertions;
using LearnPath.API.Entities;
using LearnPath.API.Services.Dashboard;
using LearnPath.Tests.Helpers;
using Xunit;

namespace LearnPath.Tests.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardDataAsync_NewUser_ReturnsZeroStats()
    {
        var context = DbContextFactory.Create();
        var userId  = Guid.NewGuid().ToString();
        var user    = EntityFactory.CreateUser(id: userId);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        var result = await service.GetDashboardDataAsync(userId);

        result.Should().NotBeNull();
        result.Stats.EnrolledPaths.Should().Be(0);
        result.Stats.CompletedModules.Should().Be(0);
        result.Stats.ActiveClassrooms.Should().Be(0);
        result.Stats.Certificates.Should().Be(0);
        result.PathProgress.Should().BeEmpty();
        result.RecentActivity.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithCompletedModules_ReturnsCorrectCount()
    {
        var context = DbContextFactory.Create();
        var userId  = Guid.NewGuid().ToString();
        var user    = EntityFactory.CreateUser(id: userId);
        await context.Users.AddAsync(user);

        var path = EntityFactory.CreateLearningPath(id: 1, createdById: userId);
        await context.LearningPaths.AddAsync(path);

        var m1 = EntityFactory.CreateModule(id: 1, learningPathId: 1);
        var m2 = EntityFactory.CreateModule(id: 2, learningPathId: 1);
        await context.Modules.AddRangeAsync(m1, m2);

        await context.Progresses.AddRangeAsync(
            new Entities.Progress
            {
                UserId = userId, ModuleId = 1,
                IsCompleted = true, CompletedAt = DateTime.UtcNow,
            },
            new Entities.Progress
            {
                UserId = userId, ModuleId = 2,
                IsCompleted = true, CompletedAt = DateTime.UtcNow,
            }
        );
        await context.SaveChangesAsync();

        var service = new DashboardService(context);

        var result = await service.GetDashboardDataAsync(userId);

        result.Stats.CompletedModules.Should().Be(2);
        result.Stats.EnrolledPaths.Should().Be(1);
        result.PathProgress.Should().HaveCount(1);
        result.PathProgress[0].CompletedModules.Should().Be(2);
    }
}
