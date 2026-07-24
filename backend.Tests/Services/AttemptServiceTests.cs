using FluentAssertions;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Attempt;
using LearnPath.API.Entities;
using LearnPath.API.Services.Attempt;
using LearnPath.API.Services.Validation;
using LearnPath.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearnPath.Tests.Services;

public class AttemptServiceTests
{
    /* ── Helpers ───────────────────────────────────────── */

    private async Task<(
        ApplicationDbContext ctx,
        AttemptService svc,
        string userId,
        int quizId,
        int moduleId)> SeedHappyPathAsync()
    {
        var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();

        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 3));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, title: "Happy Quiz", questionBankId: 1,
            questionCount: 3, status: QuizStatus.Published));
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(moduleId: 1, quizId: 1));
        await ctx.SaveChangesAsync();

        var svc = new AttemptService(ctx);
        return (ctx, svc, userId, 1, 1);
    }

    /* ── StartAttempt ──────────────────────────────────── */

    [Fact]
    public async Task StartAttemptAsync_CreatesNewAttempt()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();

        var result = await svc.StartAttemptAsync(quizId, moduleId, userId);

        result.AttemptNumber.Should().Be(1);
        result.Status.Should().Be(AttemptStatus.InProgress);
        result.Questions.Should().HaveCount(3);
    }

    [Fact]
    public async Task StartAttemptAsync_ResumesActiveAttempt()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();

        var first = await svc.StartAttemptAsync(quizId, moduleId, userId);
        var second = await svc.StartAttemptAsync(quizId, moduleId, userId);

        second.AttemptId.Should().Be(first.AttemptId);
    }

    [Fact]
    public async Task StartAttemptAsync_ExceedsMaxAttempts_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        ctx.QuizAttempts.AddRange(
            EntityFactory.CreateQuizAttempt(id: 1, userId: userId, quizId: quizId, moduleId: moduleId,
                attemptNumber: 1, status: AttemptStatus.Evaluated, passed: false),
            EntityFactory.CreateQuizAttempt(id: 2, userId: userId, quizId: quizId, moduleId: moduleId,
                attemptNumber: 2, status: AttemptStatus.Evaluated, passed: false),
            EntityFactory.CreateQuizAttempt(id: 3, userId: userId, quizId: quizId, moduleId: moduleId,
                attemptNumber: 3, status: AttemptStatus.Evaluated, passed: false)
        );
        await ctx.SaveChangesAsync();

        var act = () => svc.StartAttemptAsync(quizId, moduleId, userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Maximum attempts*");
    }

    [Fact]
    public async Task StartAttemptAsync_AlreadyPassed_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 3));
        var quiz = EntityFactory.CreateQuiz(id: 1, title: "Pass Quiz", questionBankId: 1,
            questionCount: 3, status: QuizStatus.Published);
        quiz.MaximumAttempts = 1;
        ctx.Quizzes.Add(quiz);
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(moduleId: 1, quizId: 1));
        ctx.QuizAttempts.Add(EntityFactory.CreateQuizAttempt(id: 1, userId: userId, quizId: 1,
            moduleId: 1, attemptNumber: 1, status: AttemptStatus.Evaluated, score: 3, percentage: 100, passed: true));
        await ctx.SaveChangesAsync();
        var svc = new AttemptService(ctx);

        var act = () => svc.StartAttemptAsync(1, 1, userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already passed*");
    }

    [Fact]
    public async Task StartAttemptAsync_MissingQuiz_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new AttemptService(ctx);

        var act = () => svc.StartAttemptAsync(999, 1, "user");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task StartAttemptAsync_QuizNotPublished_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1, status: QuizStatus.Draft));
        await ctx.SaveChangesAsync();
        var svc = new AttemptService(ctx);

        var act = () => svc.StartAttemptAsync(1, 1, "user");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not published*");
    }

    [Fact]
    public async Task StartAttemptAsync_NotAssigned_Throws()
    {
        using var ctx = DbContextFactory.Create();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1));
        ctx.Quizzes.Add(EntityFactory.CreateQuiz(id: 1, questionBankId: 1, status: QuizStatus.Published));
        await ctx.SaveChangesAsync();
        var svc = new AttemptService(ctx);

        var act = () => svc.StartAttemptAsync(1, 99, "user");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not assigned*");
    }

    /* ── GetAttempt (Resume) ───────────────────────────── */

    [Fact]
    public async Task GetAttemptAsync_OwnAttempt_ReturnsData()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var result = await svc.GetAttemptAsync(started.AttemptId, userId);

        result.AttemptId.Should().Be(started.AttemptId);
        result.Questions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAttemptAsync_WrongUser_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var act = () => svc.GetAttemptAsync(started.AttemptId, "other-user");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetAttemptAsync_Missing_Throws()
    {
        using var ctx = DbContextFactory.Create();
        var svc = new AttemptService(ctx);

        var act = () => svc.GetAttemptAsync(999, "user");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /* ── SaveAnswer ────────────────────────────────────── */

    [Fact]
    public async Task SaveAnswerAsync_SavesNewAnswer()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);
        var qId = started.Questions[0].QuestionId;
        var optId = started.Questions[0].Options[0].OptionId;

        await svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = qId, OptionId = optId }, userId);

        var saved = await ctx.StudentAnswers.FirstAsync();
        saved.QuestionId.Should().Be(qId);
        saved.OptionId.Should().Be(optId);
    }

    [Fact]
    public async Task SaveAnswerAsync_UpdatesExistingAnswer()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);
        var qId = started.Questions[0].QuestionId;
        var optA = started.Questions[0].Options[0].OptionId;
        var optB = started.Questions[0].Options[1].OptionId;

        await svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = qId, OptionId = optA }, userId);
        await svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = qId, OptionId = optB }, userId);

        var saved = await ctx.StudentAnswers.FirstAsync();
        saved.OptionId.Should().Be(optB);
    }

    [Fact]
    public async Task SaveAnswerAsync_SubmittedAttempt_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);
        var qId = started.Questions[0].QuestionId;
        var optId = started.Questions[0].Options[0].OptionId;
        var ctx2 = ctx;
        var attempt = await ctx2.QuizAttempts.FindAsync(started.AttemptId);
        attempt!.Status = AttemptStatus.Submitted;
        await ctx2.SaveChangesAsync();

        var act = () => svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = qId, OptionId = optId }, userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not in progress*");
    }

    /* ── SubmitAttempt ─────────────────────────────────── */

    [Fact]
    public async Task SubmitAttemptAsync_EvaluatesAnswers()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var questions = await ctx.Questions.Include(q => q.Options).ToListAsync();
        foreach (var q in questions)
        {
            var correct = q.Options.First(o => o.IsCorrect);
            await svc.SaveAnswerAsync(started.AttemptId,
                new SaveAnswerRequestDto { QuestionId = q.Id, OptionId = correct.Id }, userId);
        }

        var result = await svc.SubmitAttemptAsync(started.AttemptId, userId);

        result.Score.Should().Be(3);
        result.TotalQuestions.Should().Be(3);
        result.Passed.Should().BeTrue();

        var attempt = await ctx.QuizAttempts.FindAsync(started.AttemptId);
        attempt!.Status.Should().Be(AttemptStatus.Evaluated);
        attempt.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitAttemptAsync_AllWrong_ReturnsZeroScore()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var questions = await ctx.Questions.Include(q => q.Options).ToListAsync();
        foreach (var q in questions)
        {
            var wrong = q.Options.First(o => !o.IsCorrect);
            await svc.SaveAnswerAsync(started.AttemptId,
                new SaveAnswerRequestDto { QuestionId = q.Id, OptionId = wrong.Id }, userId);
        }

        var result = await svc.SubmitAttemptAsync(started.AttemptId, userId);

        result.Score.Should().Be(0);
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAttemptAsync_AlreadySubmitted_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);
        await svc.SubmitAttemptAsync(started.AttemptId, userId);

        var act = () => svc.SubmitAttemptAsync(started.AttemptId, userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already been submitted*");
    }

    [Fact]
    public async Task SubmitAttemptAsync_WrongUser_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var act = () => svc.SubmitAttemptAsync(started.AttemptId, "other-user");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /* ── GetReview ─────────────────────────────────────── */

    [Fact]
    public async Task GetReviewAsync_ReturnsReviewWithCorrectAndIncorrect()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var questions = await ctx.Questions.Include(q => q.Options).ToListAsync();

        await svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = questions[0].Id, OptionId = questions[0].Options.First(o => o.IsCorrect).Id }, userId);
        await svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = questions[1].Id, OptionId = questions[1].Options.First(o => !o.IsCorrect).Id }, userId);
        await svc.SaveAnswerAsync(started.AttemptId,
            new SaveAnswerRequestDto { QuestionId = questions[2].Id, OptionId = questions[2].Options.First(o => o.IsCorrect).Id }, userId);

        await svc.SubmitAttemptAsync(started.AttemptId, userId);

        var review = await svc.GetReviewAsync(started.AttemptId, userId);

        review.Score.Should().Be(2);
        review.TotalQuestions.Should().Be(3);
        review.Questions.Should().HaveCount(3);
        review.Questions.Count(q => q.IsCorrect).Should().Be(2);
        review.Questions.Count(q => !q.IsCorrect).Should().Be(1);
    }

    [Fact]
    public async Task GetReviewAsync_BeforeSubmission_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);

        var act = () => svc.GetReviewAsync(started.AttemptId, userId);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not been submitted*");
    }

    [Fact]
    public async Task GetReviewAsync_WrongUser_Throws()
    {
        var (ctx, svc, userId, quizId, moduleId) = await SeedHappyPathAsync();
        var started = await svc.StartAttemptAsync(quizId, moduleId, userId);
        await svc.SubmitAttemptAsync(started.AttemptId, userId);

        var act = () => svc.GetReviewAsync(started.AttemptId, "other-user");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /* ── Random vs Sequential selection ────────────────── */

    [Fact]
    public async Task StartAttemptAsync_SequentialMode_ReturnsQuestionsInOrder()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        var bank = EntityFactory.CreateQuestionBank(id: 1, questionCount: 5);
        ctx.QuestionBanks.Add(bank);
        var quiz = EntityFactory.CreateQuiz(id: 1, questionBankId: 1, questionCount: 3, status: QuizStatus.Published);
        quiz.SelectionMode = SelectionMode.Sequential;
        ctx.Quizzes.Add(quiz);
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(moduleId: 1, quizId: 1));
        await ctx.SaveChangesAsync();
        var svc = new AttemptService(ctx);

        var result = await svc.StartAttemptAsync(1, 1, userId);

        result.Questions.Should().HaveCount(3);
        result.Questions[0].DisplayOrder.Should().Be(1);
        result.Questions[1].DisplayOrder.Should().Be(2);
        result.Questions[2].DisplayOrder.Should().Be(3);
    }

    [Fact]
    public async Task StartAttemptAsync_RandomMode_SelectsCorrectQuestionCount()
    {
        using var ctx = DbContextFactory.Create();
        var userId = Guid.NewGuid().ToString();
        ctx.QuestionBanks.Add(EntityFactory.CreateQuestionBank(id: 1, questionCount: 5));
        var quiz = EntityFactory.CreateQuiz(id: 1, questionBankId: 1, questionCount: 4,
            status: QuizStatus.Published);
        quiz.SelectionMode = SelectionMode.Random;
        ctx.Quizzes.Add(quiz);
        ctx.Modules.Add(EntityFactory.CreateModule(id: 1));
        ctx.ModuleQuizzes.Add(EntityFactory.CreateModuleQuiz(moduleId: 1, quizId: 1));
        await ctx.SaveChangesAsync();
        var svc = new AttemptService(ctx);

        var result = await svc.StartAttemptAsync(1, 1, userId);

        result.Questions.Should().HaveCount(4);
        var ids = result.Questions.Select(q => q.QuestionId).Distinct().ToList();
        ids.Should().HaveCount(4);
    }
}
