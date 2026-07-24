using FluentAssertions;
using LearnPath.API.DTOs.QuestionBank;
using LearnPath.API.Entities;
using LearnPath.API.Services.QuestionBank;
using LearnPath.API.Services.Validation;
using LearnPath.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnPath.Tests.Services;

public class QuestionBankServiceTests
{
    /* ── Upload ────────────────────────────────────────── */

    [Fact]
    public async Task UploadAsync_ValidJson_CreatesQuestionBankWithQuestions()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());
        var json = """
            {
              "title": "Java OOP",
              "subject": "Java",
              "questions": [
                { "question": "Q1?", "options": ["A","B","C","D"], "correct_answer": "A", "difficulty": "Easy", "explanation": "E1" },
                { "question": "Q2?", "options": ["A","B","C","D"], "correct_answer": "B", "difficulty": "Medium", "explanation": "E2" }
              ]
            }
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));

        var result = await svc.UploadAsync("user1", "test.json", stream);

        result.Success.Should().BeTrue();
        result.QuestionBank.Should().NotBeNull();
        result.QuestionBank!.Title.Should().Be("Java OOP");
        result.QuestionBank.QuestionCount.Should().Be(2);
        result.QuestionBank.Version.Should().Be(1);

        var saved = await ctx.QuestionBanks.Include(qb => qb.Questions).ThenInclude(q => q.Options).FirstAsync();
        saved.Questions.Should().HaveCount(2);
        saved.Questions.First().Options.Should().HaveCount(4);
    }

    [Fact]
    public async Task UploadAsync_InvalidJson_ReturnsFailure()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());
        using var stream = new MemoryStream("invalid"u8.ToArray());

        var result = await svc.UploadAsync("user1", "test.txt", stream);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UploadAsync_DuplicateTitleSubject_IncrementsVersion()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());
        var json = """
            { "title": "Java OOP", "subject": "Java", "questions": [
              { "question": "Q1?", "options": ["A","B","C","D"], "correct_answer": "A", "difficulty": "Easy", "explanation": "E1" }
            ]}
            """;
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        await svc.UploadAsync("user1", "test.json", new MemoryStream(bytes));
        var result = await svc.UploadAsync("user1", "test.json", new MemoryStream(bytes));

        result.Success.Should().BeTrue();
        result.QuestionBank!.Version.Should().Be(2);
    }

    /* ── Search ────────────────────────────────────────── */

    [Fact]
    public async Task SearchAsync_ByTitle_ReturnsMatching()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(new QuestionBank
        {
            Id = 1, Title = "Java Basics", Subject = "CS", Version = 1,
            StoredJson = "{}", OriginalFileName = "a.json", QuestionCount = 0,
            CreatedBy = "u", CreatedAt = DateTime.UtcNow,
        });
        ctx.QuestionBanks.Add(new QuestionBank
        {
            Id = 2, Title = "C# Basics", Subject = "CS", Version = 1,
            StoredJson = "{}", OriginalFileName = "b.json", QuestionCount = 0,
            CreatedBy = "u", CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var results = await svc.SearchAsync(title: "Java", subject: null, tag: null);

        results.Should().ContainSingle(r => r.Title == "Java Basics");
    }

    [Fact]
    public async Task SearchAsync_BySubject_ReturnsMatching()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(new QuestionBank
        {
            Id = 1, Title = "Java OOP", Subject = "Java", Version = 1,
            StoredJson = "{}", OriginalFileName = "a.json", QuestionCount = 0,
            CreatedBy = "u", CreatedAt = DateTime.UtcNow,
        });
        ctx.QuestionBanks.Add(new QuestionBank
        {
            Id = 2, Title = "C# OOP", Subject = "C#", Version = 1,
            StoredJson = "{}", OriginalFileName = "b.json", QuestionCount = 0,
            CreatedBy = "u", CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var results = await svc.SearchAsync(title: null, subject: "Java", tag: null);

        results.Should().ContainSingle(r => r.Subject == "Java");
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ReturnsEmpty()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var results = await svc.SearchAsync(title: "NonExistent", subject: null, tag: null);

        results.Should().BeEmpty();
    }

    /* ── GetById ───────────────────────────────────────── */

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 99, title: "Found Me"));
        await ctx.SaveChangesAsync();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var result = await svc.GetByIdAsync(99);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Found Me");
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var result = await svc.GetByIdAsync(999);

        result.Should().BeNull();
    }

    /* ── GetStoredJson ─────────────────────────────────── */

    [Fact]
    public async Task GetStoredJsonAsync_Existing_ReturnsJsonString()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var json = await svc.GetStoredJsonAsync(1);

        json.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStoredJsonAsync_Missing_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var json = await svc.GetStoredJsonAsync(999);

        json.Should().BeNull();
    }

    /* ── UploadVersion ─────────────────────────────────── */

    [Fact]
    public async Task UploadVersionAsync_ExistingBank_CreatesNewVersion()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, title: "Java", version: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuestionBankService(ctx, new JsonValidationService());
        var json = """
            { "title": "Java", "subject": "Java", "questions": [
              { "question": "Q1?", "options": ["A","B","C","D"], "correct_answer": "A", "difficulty": "Easy", "explanation": "E1" }
            ]}
            """;

        var result = await svc.UploadVersionAsync(1, "user1", "v2.json", new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

        result.Success.Should().BeTrue();
        result.QuestionBank!.Version.Should().Be(2);
    }

    [Fact]
    public async Task UploadVersionAsync_MissingBank_ReturnsFailure()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var result = await svc.UploadVersionAsync(999, "user1", "v2.json", new MemoryStream());

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    /* ── Archive ───────────────────────────────────────── */

    [Fact]
    public async Task ArchiveAsync_ExistingBank_SetsArchivedStatus()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        await ctx.SaveChangesAsync();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var result = await svc.ArchiveAsync(1);

        result.Should().NotBeNull();
        result!.Status.Should().Be(QuestionBankStatus.Archived);
        result.ArchivedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ArchiveAsync_MissingBank_ReturnsNull()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new QuestionBankService(ctx, new JsonValidationService());

        var result = await svc.ArchiveAsync(999);

        result.Should().BeNull();
    }
}
