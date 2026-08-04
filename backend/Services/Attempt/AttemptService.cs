using LearnPath.API.Data;
using LearnPath.API.DTOs.Attempt;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Attempt;

public class AttemptService : IAttemptService
{
    private readonly ApplicationDbContext _context;
    private readonly IProgressService _progressService;
    private readonly IAuditLogService _auditLog;

    public AttemptService(
        ApplicationDbContext context,
        IProgressService progressService,
        IAuditLogService auditLog)
    {
        _context = context;
        _progressService = progressService;
        _auditLog = auditLog;
    }

    public async Task<AttemptStartResponseDto> StartAttemptAsync(int quizId, int moduleId, string userId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == quizId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        if (quiz.Status != QuizStatus.Published)
            throw new InvalidOperationException("Quiz is not published.");

        var moduleQuiz = await _context.ModuleQuizzes
            .FirstOrDefaultAsync(mq => mq.ModuleId == moduleId && mq.QuizId == quizId)
            ?? throw new InvalidOperationException("Quiz is not assigned to this module.");

        var existingAttempts = await _context.QuizAttempts
            .Where(a => a.UserId == userId && a.QuizId == quizId && a.ModuleId == moduleId)
            .OrderByDescending(a => a.AttemptNumber)
            .ToListAsync();

        var activeAttempt = existingAttempts
            .FirstOrDefault(a => a.Status == AttemptStatus.InProgress || a.Status == AttemptStatus.Created);

        if (activeAttempt is not null)
            return await BuildAttemptResponse(activeAttempt.Id, quiz);

        if (existingAttempts.Count >= quiz.MaximumAttempts)
        {
            var best = existingAttempts.MaxBy(a => a.Percentage);
            if (best?.Passed == true)
                throw new InvalidOperationException("You have already passed this quiz.");
            throw new InvalidOperationException($"Maximum attempts ({quiz.MaximumAttempts}) reached.");
        }

        var attemptNumber = existingAttempts.Count + 1;
        var randomSeed = Random.Shared.Next();

        var attempt = new QuizAttempt
        {
            UserId = userId,
            QuizId = quizId,
            ModuleId = moduleId,
            AttemptNumber = attemptNumber,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
            RandomSeed = randomSeed,
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.QUIZ_STARTED,
            "Quiz",
            quiz.Id.ToString(),
            $"User started quiz '{quiz.Title}' (attempt #{attemptNumber}).",
            additionalData: $"ModuleId: {moduleId}");

        var module = await _context.Modules.FindAsync(moduleId);
        if (module is not null && module.Status < ModuleStatus.QuizAttempted)
        {
            module.Status = ModuleStatus.QuizAttempted;
            await _context.SaveChangesAsync();
        }

        return await BuildAttemptResponse(attempt.Id, quiz);
    }

    public async Task<AttemptStartResponseDto> GetAttemptAsync(int attemptId, string userId)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.QuestionBank)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException("Attempt not found.");

        if (attempt.UserId != userId)
            throw new UnauthorizedAccessException("This attempt does not belong to you.");

        return await BuildAttemptResponse(attempt.Id, attempt.Quiz);
    }

    public async Task SaveAnswerAsync(int attemptId, SaveAnswerRequestDto dto, string userId)
    {
        var attempt = await _context.QuizAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException("Attempt not found.");

        if (attempt.UserId != userId)
            throw new UnauthorizedAccessException("This attempt does not belong to you.");

        if (attempt.Status != AttemptStatus.InProgress && attempt.Status != AttemptStatus.Created)
            throw new InvalidOperationException("Attempt is not in progress.");

        if (attempt.Status == AttemptStatus.Created)
            attempt.Status = AttemptStatus.InProgress;

        var existing = await _context.StudentAnswers
            .FirstOrDefaultAsync(sa => sa.QuizAttemptId == attemptId && sa.QuestionId == dto.QuestionId);

        if (existing is not null)
        {
            existing.OptionId = dto.OptionId;
            existing.AnsweredAt = DateTime.UtcNow;
        }
        else
        {
            _context.StudentAnswers.Add(new StudentAnswer
            {
                QuizAttemptId = attemptId,
                QuestionId = dto.QuestionId,
                OptionId = dto.OptionId,
                AnsweredAt = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<SubmitResponseDto> SubmitAttemptAsync(int attemptId, string userId)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException("Attempt not found.");

        if (attempt.UserId != userId)
            throw new UnauthorizedAccessException("This attempt does not belong to you.");

        if (attempt.Status != AttemptStatus.InProgress && attempt.Status != AttemptStatus.Created)
            throw new InvalidOperationException("Attempt has already been submitted.");

        var quiz = attempt.Quiz;
        var questions = await GetSelectedQuestionsAsync(quiz, attempt.RandomSeed);
        var totalQuestions = questions.Count;

        var correctCount = 0;
        foreach (var question in questions)
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            if (answer is not null)
            {
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                if (correctOption is not null && answer.OptionId == correctOption.Id)
                    correctCount++;
            }
        }

        var timeSpent = (int)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds;

        attempt.Status = AttemptStatus.Evaluated;
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.TimeSpentSeconds = timeSpent;
        attempt.Score = correctCount;
        attempt.Percentage = totalQuestions > 0
            ? Math.Round((decimal)correctCount / totalQuestions * 100, 2)
            : 0;
        attempt.Passed = attempt.Percentage >= quiz.PassingPercentage;

        if (attempt.Passed == true)
        {
            var module = await _context.Modules.FindAsync(attempt.ModuleId);
            if (module is not null && module.Status < ModuleStatus.Completed)
                module.Status = ModuleStatus.Completed;
        }

        await _context.SaveChangesAsync();

        if (attempt.Passed == true)
            await _progressService.MarkModuleCompleteFromQuizAsync(userId, attempt.ModuleId);

        await _auditLog.LogAsync(
            AuditAction.QUIZ_COMPLETED,
            "Quiz",
            quiz.Id.ToString(),
            $"User completed quiz '{quiz.Title}' with {attempt.Percentage:0.##}%.",
            additionalData: $"AttemptId: {attempt.Id}");

        await _auditLog.LogAsync(
            attempt.Passed == true ? AuditAction.QUIZ_PASSED : AuditAction.QUIZ_FAILED,
            "Quiz",
            quiz.Id.ToString(),
            $"User {(attempt.Passed == true ? "passed" : "failed")} quiz '{quiz.Title}' with {attempt.Percentage:0.##}%.",
            additionalData: $"AttemptId: {attempt.Id}");

        return new SubmitResponseDto
        {
            AttemptId = attempt.Id,
            Score = correctCount,
            TotalQuestions = totalQuestions,
            Percentage = attempt.Percentage.Value,
            Passed = attempt.Passed.Value,
            TimeSpentSeconds = timeSpent,
            AttemptNumber = attempt.AttemptNumber,
            PassingPercentage = quiz.PassingPercentage,
        };
    }

    public async Task<ReviewResponseDto> GetReviewAsync(int attemptId, string userId)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Quiz).ThenInclude(q => q.QuestionBank)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId)
            ?? throw new KeyNotFoundException("Attempt not found.");

        if (attempt.UserId != userId)
            throw new UnauthorizedAccessException("This attempt does not belong to you.");

        if (attempt.Status < AttemptStatus.Submitted)
            throw new InvalidOperationException("Attempt has not been submitted yet.");

        var quiz = attempt.Quiz;
        var questions = await GetSelectedQuestionsAsync(quiz, attempt.RandomSeed);
        var totalQuestions = questions.Count;

        var reviewQuestions = questions.Select(q =>
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var correctOption = q.Options.First(o => o.IsCorrect);
            var selectedOptionId = answer?.OptionId;

            return new ReviewQuestionDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                Explanation = q.Explanation,
                SelectedOptionId = selectedOptionId,
                CorrectOptionId = correctOption.Id,
                IsCorrect = selectedOptionId == correctOption.Id,
                Options = ShuffleList(q.Options.ToList(), attempt.RandomSeed + q.Id)
                    .Select((o, idx) => new ReviewOptionDto
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                        IsSelected = o.Id == selectedOptionId,
                        DisplayOrder = idx + 1,
                    }).ToList(),
            };
        }).ToList();

        return new ReviewResponseDto
        {
            AttemptId = attempt.Id,
            Score = attempt.Score ?? 0,
            TotalQuestions = totalQuestions,
            Percentage = attempt.Percentage ?? 0,
            Passed = attempt.Passed ?? false,
            TimeSpentSeconds = attempt.TimeSpentSeconds ?? 0,
            AttemptNumber = attempt.AttemptNumber,
            PassingPercentage = quiz.PassingPercentage,
            Questions = reviewQuestions,
        };
    }

    private async Task<AttemptStartResponseDto> BuildAttemptResponse(int attemptId, Entities.Quiz quiz)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.Answers)
            .FirstAsync(a => a.Id == attemptId);

        var questions = await GetSelectedQuestionsAsync(quiz, attempt.RandomSeed);

        var questionDtos = questions.Select((q, i) =>
        {
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            return new AttemptQuestionDto
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                DisplayOrder = i + 1,
                SelectedOptionId = answer?.OptionId,
                Options = ShuffleList(q.Options.ToList(), attempt.RandomSeed + q.Id)
                    .Select((o, idx) => new AttemptOptionDto
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText,
                        DisplayOrder = idx + 1,
                    }).ToList(),
            };
        }).ToList();

        return new AttemptStartResponseDto
        {
            AttemptId = attempt.Id,
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            ModuleId = attempt.ModuleId,
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            TimeSpentSeconds = attempt.TimeSpentSeconds,
            Questions = questionDtos,
        };
    }

    private async Task<List<Question>> GetSelectedQuestionsAsync(Entities.Quiz quiz, int seed)
    {
        var query = _context.Questions
            .Include(q => q.Options)
            .Where(q => q.QuestionBankId == quiz.QuestionBankId);

        if (quiz.DifficultyFilter.HasValue)
            query = query.Where(q => q.Difficulty == quiz.DifficultyFilter.Value);

        var allQuestions = await query.OrderBy(q => q.Id).ToListAsync();

        if (quiz.SelectionMode == SelectionMode.Random)
            return ShuffleList(allQuestions, seed).Take(quiz.QuestionCount).ToList();

        return allQuestions.Take(quiz.QuestionCount).ToList();
    }

    private static List<T> ShuffleList<T>(List<T> list, int seed)
    {
        var rng = new Random(seed);
        var shuffled = list.ToList();
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }
}
