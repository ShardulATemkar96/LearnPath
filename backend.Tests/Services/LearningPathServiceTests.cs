using FluentAssertions;
using LearnPath.API.Data;
using LearnPath.API.Entities;
using LearnPath.API.Services.LearningPath;
using LearnPath.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnPath.Tests.Services;

public class LearningPathServiceTests
{
    private static (ApplicationDbContext ctx, LearningPathService svc, string ownerId) CreateService()
    {
        var ctx = DbContextFactory.Create();
        var ownerId = Guid.NewGuid().ToString();
        var svc = new LearningPathService(
            ctx,
            new FakeAuditLogService(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LearningPathService>.Instance);
        return (ctx, svc, ownerId);
    }

    /* ── DeleteAsync ─────────────────────────────────────── */

    [Fact]
    public async Task DeleteAsync_EmptyPath_DeletesPath()
    {
        var (ctx, svc, ownerId) = CreateService();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        await ctx.SaveChangesAsync();

        await svc.DeleteAsync(1, ownerId);

        (await ctx.LearningPaths.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_PathWithModules_DeletesPathAndModules()
    {
        var (ctx, svc, ownerId) = CreateService();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, title: "M1", order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, title: "M2", order: 2));
        await ctx.SaveChangesAsync();

        await svc.DeleteAsync(1, ownerId);

        (await ctx.LearningPaths.CountAsync()).Should().Be(0);
        (await ctx.Modules.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_PathWithDependencies_ClearsDependenciesAndDeletes()
    {
        var (ctx, svc, ownerId) = CreateService();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, title: "M1", order: 1));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 2, learningPathId: 1, title: "M2", order: 2));
        ctx.ModuleDependencies.Add(new ModuleDependency { ModuleId = 2, DependsOnModuleId = 1 });
        await ctx.SaveChangesAsync();

        await svc.DeleteAsync(1, ownerId);

        (await ctx.LearningPaths.CountAsync()).Should().Be(0);
        (await ctx.Modules.CountAsync()).Should().Be(0);
        (await ctx.ModuleDependencies.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_PathWithQuizAttempts_DeletesAttemptsAndPath()
    {
        var (ctx, svc, ownerId) = CreateService();
        var studentId = Guid.NewGuid().ToString();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.Users.Add(EntityFactory.CreateUser(id: studentId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, title: "M1", order: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(moduleId: 1, quizId: 1));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 1, userId: studentId, quizId: 1, moduleId: 1));
        await ctx.SaveChangesAsync();

        await svc.DeleteAsync(1, ownerId);

        (await ctx.LearningPaths.CountAsync()).Should().Be(0);
        (await ctx.Modules.CountAsync()).Should().Be(0);
        (await ctx.QuizAttempts.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_PathWithClassroom_ThrowsAndKeepsPath()
    {
        var (ctx, svc, ownerId) = CreateService();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        ctx.Classrooms.Add(EntityFactory.CreateClassroom(id: 1, createdById: ownerId, learningPathId: 1));
        await ctx.SaveChangesAsync();

        var act = async () => await svc.DeleteAsync(1, ownerId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*classroom*");
        (await ctx.LearningPaths.CountAsync()).Should().Be(1);
        (await ctx.Classrooms.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_PathWithCertificate_ThrowsAndKeepsPath()
    {
        var (ctx, svc, ownerId) = CreateService();
        var studentId = Guid.NewGuid().ToString();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.Users.Add(EntityFactory.CreateUser(id: studentId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        ctx.Certificates.Add(new Certificate
        {
            Id = 1,
            UserId = studentId,
            LearningPathId = 1,
            CertificateUrl = "https://example.com/cert.pdf",
        });
        await ctx.SaveChangesAsync();

        var act = async () => await svc.DeleteAsync(1, ownerId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*certificate*");
        (await ctx.LearningPaths.CountAsync()).Should().Be(1);
        (await ctx.Certificates.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentPath_ThrowsKeyNotFound()
    {
        var (ctx, svc, ownerId) = CreateService();

        var act = async () => await svc.DeleteAsync(999, ownerId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsUnauthorized()
    {
        var (ctx, svc, ownerId) = CreateService();
        var otherId = Guid.NewGuid().ToString();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.Users.Add(EntityFactory.CreateUser(id: otherId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        await ctx.SaveChangesAsync();

        var act = async () => await svc.DeleteAsync(1, otherId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        (await ctx.LearningPaths.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEveryDependentRowExplicitly()
    {
        var (ctx, svc, ownerId) = CreateService();
        var studentId = Guid.NewGuid().ToString();
        ctx.Users.Add(EntityFactory.CreateUser(id: ownerId));
        ctx.Users.Add(EntityFactory.CreateUser(id: studentId));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: ownerId));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1, learningPathId: 1, title: "M1", order: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(id: 1, moduleId: 1, quizId: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1));
        ctx.Progresses.Add(new Progress { Id = 1, UserId = studentId, ModuleId = 1, IsCompleted = true });
        ctx.ModuleObjectives.Add(new ModuleObjective { Id = 1, ModuleId = 1, ObjectiveText = "Goal" });
        ctx.ModuleResources.Add(new ModuleResource { Id = 1, ModuleId = 1, Title = "Doc", Url = "https://example.com", Type = "link" });
        ctx.ModuleTags.Add(new ModuleTag { Id = 1, ModuleId = 1, TagName = "java" });
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 1, userId: studentId, quizId: 1, moduleId: 1));
        ctx.StudentAnswers.Add(EntityFactory.CreateStudentAnswer(id: 1, attemptId: 1, questionId: 1, optionId: 11));
        await ctx.SaveChangesAsync();

        await svc.DeleteAsync(1, ownerId);

        (await ctx.LearningPaths.CountAsync()).Should().Be(0);
        (await ctx.Modules.CountAsync()).Should().Be(0);
        (await ctx.ModuleQuizzes.CountAsync()).Should().Be(0);
        (await ctx.Progresses.CountAsync()).Should().Be(0);
        (await ctx.ModuleObjectives.CountAsync()).Should().Be(0);
        (await ctx.ModuleResources.CountAsync()).Should().Be(0);
        (await ctx.ModuleTags.CountAsync()).Should().Be(0);
        (await ctx.QuizAttempts.CountAsync()).Should().Be(0);
        (await ctx.StudentAnswers.CountAsync()).Should().Be(0);
    }
}
