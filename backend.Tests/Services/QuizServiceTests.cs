using FluentAssertions;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Entities;
using LearnPath.API.Services.Quiz;
using LearnPath.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace LearnPath.Tests.Services;

public class QuizServiceTests
{
    private static async Task SeedAdminAsync(ApplicationDbContext ctx, string userId)
    {
        var role = new IdentityRole("Admin") { Id = Guid.NewGuid().ToString() };
        await ctx.Roles.AddAsync(role);
        await ctx.UserRoles.AddAsync(new IdentityUserRole<string>
        {
            UserId = userId,
            RoleId = role.Id,
        });
        await ctx.SaveChangesAsync();
    }

    /* ── Create ────────────────────────────────────────── */

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesQuiz()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 10));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());
        var dto = new CreateQuizDto
        {
            Title = "Test Quiz",
            QuestionBankId = 1,
            QuestionCount = 5,
            PassingPercentage = 60,
            MaximumAttempts = 2,
            TimeLimitMinutes = 20,
        };

        var result = await svc.CreateAsync(dto, Guid.NewGuid().ToString());

        result.Title.Should().Be("Test Quiz");
        result.QuestionCount.Should().Be(5);
        result.PassingPercentage.Should().Be(60);
        result.MaximumAttempts.Should().Be(2);
        result.TimeLimitMinutes.Should().Be(20);
        result.Status.Should().Be(QuizStatus.Draft);
    }

    [Fact]
    public async Task CreateAsync_MissingBank_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.CreateAsync(new CreateQuizDto { Title = "Q", QuestionBankId = 999, QuestionCount = 1 }, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Question Bank not found*");
    }

    [Fact]
    public async Task CreateAsync_QuestionCountExceedsBank_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 3));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.CreateAsync(new CreateQuizDto { Title = "Q", QuestionBankId = 1, QuestionCount = 10 }, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*only has 3*");
    }

    [Fact]
    public async Task CreateAsync_ZeroQuestionCount_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.CreateAsync(new CreateQuizDto { Title = "Q", QuestionBankId = 1, QuestionCount = 0 }, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than 0*");
    }

    [Fact]
    public async Task CreateAsync_DuplicateTitle_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, title: "Duplicate"));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.CreateAsync(new CreateQuizDto { Title = "Duplicate", QuestionBankId = 1, QuestionCount = 1 }, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_DifficultyFilterWithInsufficientQuestions_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 10));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.CreateAsync(new CreateQuizDto
        {
            Title = "Q",
            QuestionBankId = 1,
            QuestionCount = 10,
            DifficultyFilter = Difficulty.Hard,
        }, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Only*");
    }

    /* ── GetAll ────────────────────────────────────────── */

    [Fact]
    public async Task GetAllAsync_ReturnsAllQuizzes()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, title: "Quiz A"));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 2, title: "Quiz B"));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());
        var userId = Guid.NewGuid().ToString();
        await SeedAdminAsync(ctx, userId);

        var results = await svc.GetAllAsync(userId);

        results.Should().HaveCount(2);
    }

    /* ── GetById ───────────────────────────────────────── */

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsQuiz()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, title: "Found"));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var result = await svc.GetByIdAsync(1);

        result.Title.Should().Be("Found");
    }

    [Fact]
    public async Task GetByIdAsync_Missing_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.GetByIdAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /* ── Update ────────────────────────────────────────── */

    [Fact]
    public async Task UpdateAsync_ValidDto_UpdatesQuiz()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 10));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, title: "Original", questionBankId: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());
        var userId = Guid.NewGuid().ToString();
        await SeedAdminAsync(ctx, userId);

        var result = await svc.UpdateAsync(1, new UpdateQuizDto
        {
            Title = "Updated",
            QuestionBankId = 1,
            QuestionCount = 3,
            PassingPercentage = 50,
            MaximumAttempts = 5,
        }, userId);

        result.Title.Should().Be("Updated");
        result.QuestionCount.Should().Be(3);
        result.PassingPercentage.Should().Be(50);
        result.MaximumAttempts.Should().Be(5);
    }

    [Fact]
    public async Task UpdateAsync_MissingQuiz_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.UpdateAsync(999, new UpdateQuizDto { Title = "X", QuestionBankId = 1, QuestionCount = 1 }, Guid.NewGuid().ToString());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /* ── Archive ───────────────────────────────────────── */

    [Fact]
    public async Task ArchiveAsync_Existing_SetsArchived()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());
        var userId = Guid.NewGuid().ToString();
        await SeedAdminAsync(ctx, userId);

        var result = await svc.ArchiveAsync(1, userId);

        result.Should().NotBeNull();
        result!.Status.Should().Be(QuizStatus.Archived);
    }

    [Fact]
    public async Task ArchiveAsync_Missing_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var result = await svc.ArchiveAsync(999, Guid.NewGuid().ToString());

        result.Should().BeNull();
    }

    /* ── Module linking ────────────────────────────────── */

    [Fact]
    public async Task LinkToModuleAsync_Valid_AddsModuleQuiz()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1, createdById: "admin"));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: "admin"));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var result = await svc.LinkToModuleAsync(1, 1, "admin");

        result.ModuleId.Should().Be(1);
        result.QuizId.Should().Be(1);
        result.Active.Should().BeTrue();
    }

    [Fact]
    public async Task LinkToModuleAsync_MissingQuiz_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.LinkToModuleAsync(1, 999, "admin");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task LinkToModuleAsync_Reassign_UpdatesExistingLink()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1, createdById: "admin"));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 2, questionBankId: 1, createdById: "admin"));
        ctx.LearningPaths.Add(EntityFactory.CreateLearningPath(id: 1, createdById: "admin"));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(id: 1, moduleId: 1, quizId: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var result = await svc.LinkToModuleAsync(1, 2, "admin");

        result.QuizId.Should().Be(2);
    }

    [Fact]
    public async Task UnlinkFromModuleAsync_Existing_RemovesLink()
    {
        using var ctx = DbContextFactory.Create();
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(id: 1, moduleId: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        await svc.UnlinkFromModuleAsync(1);

        ctx.ModuleQuizzes.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlinkFromModuleAsync_Missing_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var act = () => svc.UnlinkFromModuleAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetModuleQuizAsync_Existing_ReturnsLink()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(id: 1, moduleId: 1, quizId: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var result = await svc.GetModuleQuizAsync(1);

        result.Should().NotBeNull();
        result!.ModuleId.Should().Be(1);
    }

    [Fact]
    public async Task GetModuleQuizAsync_Missing_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuizService(ctx, new FakeAuditLogService());

        var result = await svc.GetModuleQuizAsync(999);

        result.Should().BeNull();
    }
}
