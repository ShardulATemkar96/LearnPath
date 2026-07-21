using AutoMapper;
using AutoMapper.QueryableExtensions;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Quiz;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public QuizService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // ── Admin: Quiz CRUD ───────────────────────────────────────

    public async Task<QuizResponseDto?> GetQuizByModuleAsync(int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.Quizzes)
                .ThenInclude(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                    .ThenInclude(o => o.Options.OrderBy(oo => oo.OrderIndex))
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        var quiz = module?.Quizzes.FirstOrDefault();
        return quiz is null ? null : _mapper.Map<QuizResponseDto>(quiz);
    }

    public async Task<QuizResponseDto> CreateQuizAsync(int moduleId, CreateQuizDto dto)
    {
        var moduleExists = await _context.Modules.AnyAsync(m => m.Id == moduleId);
        if (!moduleExists)
            throw new KeyNotFoundException($"Module {moduleId} not found.");

        var existing = await _context.Quizzes.AnyAsync(q => q.ModuleId == moduleId);
        if (existing)
            throw new InvalidOperationException("A quiz already exists for this module.");

        var quiz = _mapper.Map<Entities.Quiz>(dto);
        quiz.ModuleId = moduleId;
        quiz.PassingScore ??= 0;
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return _mapper.Map<QuizResponseDto>(quiz);
    }

    public async Task<QuizResponseDto?> UpdateQuizAsync(int quizId, CreateQuizDto dto)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                .ThenInclude(o => o.Options.OrderBy(oo => oo.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null) return null;

        _mapper.Map(dto, quiz);
        await _context.SaveChangesAsync();

        return _mapper.Map<QuizResponseDto>(quiz);
    }

    public async Task<bool> DeleteQuizAsync(int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz is null) return false;

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Admin: Question CRUD ───────────────────────────────────

    public async Task<AdminQuestionResponseDto> AddQuestionAsync(int quizId, CreateQuestionDto dto)
    {
        var quizExists = await _context.Quizzes.AnyAsync(q => q.Id == quizId);
        if (!quizExists)
            throw new KeyNotFoundException($"Quiz {quizId} not found.");

        if (dto.Options.Count < 1)
            throw new InvalidOperationException("A question must have at least one option.");

        var question = _mapper.Map<QuizQuestion>(dto);
        question.QuizId = quizId;
        _context.QuizQuestions.Add(question);
        await _context.SaveChangesAsync();

        return _mapper.Map<AdminQuestionResponseDto>(question);
    }

    public async Task<AdminQuestionResponseDto?> UpdateQuestionAsync(int questionId, CreateQuestionDto dto)
    {
        var question = await _context.QuizQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == questionId);

        if (question is null) return null;

        foreach (var opt in question.Options.ToList())
            _context.QuizOptions.Remove(opt);

        _mapper.Map(dto, question);
        await _context.SaveChangesAsync();

        return _mapper.Map<AdminQuestionResponseDto>(question);
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var question = await _context.QuizQuestions.FindAsync(questionId);
        if (question is null) return false;

        _context.QuizQuestions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Learner: Attempt flow ──────────────────────────────────

    public async Task<StartAttemptResponseDto?> StartAttemptAsync(int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                .ThenInclude(o => o.Options.OrderBy(oo => oo.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null) return null;

        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            Status = AttemptStatus.InProgress,
            StartedAt = DateTime.UtcNow,
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        var response = new StartAttemptResponseDto
        {
            AttemptId = attempt.Id,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            Questions = _mapper.Map<List<AttemptQuestionDto>>(quiz.Questions),
        };

        if (quiz.ShuffleQuestions)
            response.Questions = response.Questions.OrderBy(_ => Random.Shared.Next()).ToList();

        return response;
    }

    public async Task<AttemptResultDto?> SubmitAttemptAsync(SubmitAnswersDto dto)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
                .ThenInclude(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                    .ThenInclude(o => o.Options)
            .FirstOrDefaultAsync(a => a.Id == dto.AttemptId);

        if (attempt is null) return null;
        if (attempt.Status != AttemptStatus.InProgress)
            throw new InvalidOperationException("This attempt has already been submitted.");

        attempt.CompletedAt = DateTime.UtcNow;
        attempt.Status = AttemptStatus.Completed;

        var answers = new List<QuizAnswer>();
        int score = 0;
        int totalPoints = 0;
        var questionResults = new List<QuestionResultDto>();

        foreach (var question in attempt.Quiz.Questions)
        {
            totalPoints += question.Points;
            var submitted = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            bool isCorrect = false;
            int pointsAwarded = 0;
            int? correctOptionId = null;
            string? correctAnswerText = null;

            if (question.QuestionType == QuizQuestionType.ShortAnswer)
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                correctAnswerText = correctOption?.OptionText;

                if (submitted?.TextAnswer is not null && correctOption is not null)
                {
                    isCorrect = submitted.TextAnswer.Trim()
                        .Equals(correctOption.OptionText.Trim(), StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                correctOptionId = question.Options.FirstOrDefault(o => o.IsCorrect)?.Id;

                if (submitted?.SelectedOptionId is not null)
                {
                    var selectedOption = question.Options
                        .FirstOrDefault(o => o.Id == submitted.SelectedOptionId.Value);
                    isCorrect = selectedOption?.IsCorrect ?? false;
                }
            }

            if (isCorrect)
                pointsAwarded = question.Points;

            score += pointsAwarded;

            var answer = new QuizAnswer
            {
                QuizAttemptId = attempt.Id,
                QuizQuestionId = question.Id,
                SelectedOptionId = submitted?.SelectedOptionId,
                TextAnswer = submitted?.TextAnswer,
                IsCorrect = isCorrect,
                PointsAwarded = pointsAwarded,
            };
            answers.Add(answer);

            questionResults.Add(new QuestionResultDto
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Points = question.Points,
                PointsAwarded = pointsAwarded,
                IsCorrect = isCorrect,
                SelectedOptionId = submitted?.SelectedOptionId,
                CorrectOptionId = correctOptionId,
                TextAnswer = submitted?.TextAnswer,
                CorrectAnswerText = correctAnswerText,
                Explanation = question.Explanation,
            });
        }

        attempt.Score = score;
        attempt.TotalPoints = totalPoints;
        attempt.IsPassed = score >= (attempt.Quiz.PassingScore ?? 0);

        _context.QuizAnswers.AddRange(answers);
        await _context.SaveChangesAsync();

        return new AttemptResultDto
        {
            AttemptId = attempt.Id,
            Score = score,
            TotalPoints = totalPoints,
            IsPassed = attempt.IsPassed ?? false,
            Status = "Completed",
            QuestionResults = questionResults,
        };
    }

    public async Task<AttemptResultDto?> GetAttemptResultAsync(int attemptId)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
                    .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt is null) return null;

        int totalPoints = attempt.Answers.Sum(a => a.Question?.Points ?? 0);
        var questionResults = attempt.Answers.Select(a =>
        {
            int? correctOptionId = null;
            string? correctAnswerText = null;

            if (a.Question is not null)
            {
                if (a.Question.QuestionType == QuizQuestionType.ShortAnswer)
                {
                    correctAnswerText = a.Question.Options
                        .FirstOrDefault(o => o.IsCorrect)?.OptionText;
                }
                else
                {
                    correctOptionId = a.Question.Options
                        .FirstOrDefault(o => o.IsCorrect)?.Id;
                }
            }

            return new QuestionResultDto
            {
                QuestionId = a.QuizQuestionId,
                QuestionText = a.Question?.QuestionText ?? "",
                QuestionType = a.Question?.QuestionType ?? QuizQuestionType.MultipleChoice,
                Points = a.Question?.Points ?? 0,
                PointsAwarded = (a.IsCorrect ?? false) ? (a.Question?.Points ?? 0) : 0,
                IsCorrect = a.IsCorrect ?? false,
                SelectedOptionId = a.SelectedOptionId,
                CorrectOptionId = correctOptionId,
                TextAnswer = a.TextAnswer,
                CorrectAnswerText = correctAnswerText,
                Explanation = a.Question?.Explanation,
            };
        }).ToList();

        return new AttemptResultDto
        {
            AttemptId = attempt.Id,
            Score = attempt.Score ?? 0,
            TotalPoints = totalPoints,
            IsPassed = attempt.IsPassed ?? false,
            Status = attempt.Status.ToString(),
            QuestionResults = [.. questionResults],
        };
    }

    public async Task<List<AttemptSummaryDto>> GetAttemptHistoryAsync(int quizId)
    {
        return await _context.QuizAttempts
            .Where(a => a.QuizId == quizId)
            .OrderByDescending(a => a.StartedAt)
            .ProjectTo<AttemptSummaryDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    // ── Availability ───────────────────────────────────────────

    public async Task<(bool Available, string? Reason)> CheckQuizAvailableAsync(int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.Quizzes)
            .FirstOrDefaultAsync(m => m.Id == moduleId);

        if (module is null)
            return (false, "Module not found.");

        if (!module.QuizEnabled)
            return (false, "Quiz is not enabled for this module.");

        var quiz = module.Quizzes.FirstOrDefault();
        if (quiz is null)
            return (false, "No quiz configured for this module.");

        return (true, null);
    }
}
