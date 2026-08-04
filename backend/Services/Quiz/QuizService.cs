using LearnPath.API.Data;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Quiz;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public QuizService(
        ApplicationDbContext context,
        IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<List<QuizResponseDto>> GetAllAsync(string userId)
    {
        if (await IsAdminAsync(userId))
        {
            return await _context.Quizzes
                .Include(q => q.QuestionBank)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => MapToDto(q))
                .ToListAsync();
        }

        return await _context.Quizzes
            .Include(q => q.QuestionBank)
            .Where(q => q.CreatedById == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => MapToDto(q))
            .ToListAsync();
    }

    public async Task<QuizResponseDto> GetByIdAsync(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new KeyNotFoundException("Quiz not found.");

        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto> CreateAsync(CreateQuizDto dto, string userId)
    {
        var bank = await _context.QuestionBanks
            .FirstOrDefaultAsync(qb => qb.Id == dto.QuestionBankId)
            ?? throw new ArgumentException("Question Bank not found.");

        if (dto.QuestionCount <= 0)
            throw new ArgumentException("Question count must be greater than 0.");

        if (dto.QuestionCount > bank.QuestionCount)
            throw new ArgumentException($"Question Bank only has {bank.QuestionCount} questions.");

        if (dto.DifficultyFilter.HasValue)
        {
            var matchingCount = await _context.Questions
                .CountAsync(q => q.QuestionBankId == dto.QuestionBankId && q.Difficulty == dto.DifficultyFilter.Value);

            if (dto.QuestionCount > matchingCount)
                throw new ArgumentException(
                    $"Only {matchingCount} questions match the selected difficulty. Requested: {dto.QuestionCount}.");
        }

        if (await _context.Quizzes.AnyAsync(q => q.Title == dto.Title))
            throw new ArgumentException("A quiz with this title already exists.");

        var quiz = new Entities.Quiz
        {
            Title = dto.Title,
            QuestionBankId = dto.QuestionBankId,
            QuestionCount = dto.QuestionCount,
            DifficultyFilter = dto.DifficultyFilter,
            SelectionMode = dto.SelectionMode,
            TimeLimitMinutes = dto.TimeLimitMinutes,
            PassingPercentage = dto.PassingPercentage,
            MaximumAttempts = dto.MaximumAttempts,
            Status = QuizStatus.Draft,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        await LogQuizAsync(AuditAction.QUIZ_CREATED, quiz, userId,
            $"Quiz '{quiz.Title}' was created.");

        return await GetByIdAsync(quiz.Id);
    }

    public async Task<QuizResponseDto> UpdateAsync(int id, UpdateQuizDto dto, string userId)
    {
        var quiz = await GetOwnedQuizAsync(id, userId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        if (await _context.Quizzes.AnyAsync(q => q.Title == dto.Title && q.Id != id))
            throw new ArgumentException("A quiz with this title already exists.");

        var bank = await _context.QuestionBanks
            .FirstOrDefaultAsync(qb => qb.Id == dto.QuestionBankId)
            ?? throw new ArgumentException("Question Bank not found.");

        if (dto.QuestionCount <= 0)
            throw new ArgumentException("Question count must be greater than 0.");

        if (dto.QuestionCount > bank.QuestionCount)
            throw new ArgumentException($"Question Bank only has {bank.QuestionCount} questions.");

        var oldValue = $"Title: {quiz.Title}; QuestionBankId: {quiz.QuestionBankId}; QuestionCount: {quiz.QuestionCount}";

        quiz.Title = dto.Title;
        quiz.QuestionBankId = dto.QuestionBankId;
        quiz.QuestionCount = dto.QuestionCount;
        quiz.DifficultyFilter = dto.DifficultyFilter;
        quiz.SelectionMode = dto.SelectionMode;
        quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
        quiz.PassingPercentage = dto.PassingPercentage;
        quiz.MaximumAttempts = dto.MaximumAttempts;

        await _context.SaveChangesAsync();

        await LogQuizAsync(AuditAction.QUIZ_UPDATED, quiz, userId,
            $"Quiz '{quiz.Title}' was updated.",
            oldValue: oldValue,
            newValue: $"Title: {quiz.Title}; QuestionBankId: {quiz.QuestionBankId}; QuestionCount: {quiz.QuestionCount}");

        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> ArchiveAsync(int id, string userId)
    {
        var quiz = await GetOwnedQuizAsync(id, userId);
        if (quiz is null) return null;

        quiz.Status = QuizStatus.Archived;
        quiz.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await LogQuizAsync(AuditAction.QUIZ_ARCHIVED, quiz, userId,
            $"Quiz '{quiz.Title}' was archived.");

        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> PublishAsync(int id, string userId)
    {
        var quiz = await GetOwnedQuizAsync(id, userId);
        if (quiz is null) return null;

        if (quiz.Status == QuizStatus.Archived)
            throw new InvalidOperationException("Cannot publish an archived quiz.");

        quiz.Status = QuizStatus.Published;
        quiz.PublishedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await LogQuizAsync(AuditAction.QUIZ_PUBLISHED, quiz, userId,
            $"Quiz '{quiz.Title}' was published.");

        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> UnpublishAsync(int id, string userId)
    {
        var quiz = await GetOwnedQuizAsync(id, userId);
        if (quiz is null) return null;

        if (quiz.Status != QuizStatus.Published)
            throw new InvalidOperationException("Only published quizzes can be unpublished.");

        quiz.Status = QuizStatus.Draft;
        quiz.PublishedAt = null;

        await _context.SaveChangesAsync();

        await LogQuizAsync(AuditAction.QUIZ_UNPUBLISHED, quiz, userId,
            $"Quiz '{quiz.Title}' was unpublished.");

        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> DeleteAsync(int id, string userId)
    {
        var quiz = await GetOwnedQuizAsync(id, userId, includeModuleQuizzes: true);
        if (quiz is null) return null;

        if (quiz.ModuleQuizzes.Count > 0)
            throw new InvalidOperationException(
                $"Cannot delete quiz '{quiz.Title}' because it is assigned to {quiz.ModuleQuizzes.Count} module(s). Unlink it first.");

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        await LogQuizAsync(AuditAction.QUIZ_DELETED, quiz, userId,
            $"Quiz '{quiz.Title}' was deleted.");

        return MapToDto(quiz);
    }

    public async Task<ModuleQuizResponseDto> LinkToModuleAsync(int moduleId, int quizId, string userId)
    {
        var module = await _context.Modules
            .Include(m => m.LearningPath)
            .FirstOrDefaultAsync(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException("Module not found.");

        if (!await IsAdminAsync(userId) && module.LearningPath.CreatedById != userId)
            throw new UnauthorizedAccessException("You do not own this learning path.");

        var quiz = await GetOwnedQuizAsync(quizId, userId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        if (quiz.Status != QuizStatus.Published)
            throw new InvalidOperationException("Quiz must be published before assignment.");

        var existing = await _context.ModuleQuizzes
            .FirstOrDefaultAsync(mq => mq.ModuleId == moduleId);

        if (existing is not null)
        {
            existing.QuizId = quizId;
            existing.AssignedBy = userId;
            existing.AssignedAt = DateTime.UtcNow;
            existing.Active = true;
        }
        else
        {
            var moduleQuiz = new Entities.ModuleQuiz
            {
                ModuleId = moduleId,
                QuizId = quizId,
                AssignedBy = userId,
                AssignedAt = DateTime.UtcNow,
                Active = true,
            };
            _context.ModuleQuizzes.Add(moduleQuiz);
        }

        await _context.SaveChangesAsync();

        var linkedQuiz = await _context.Quizzes.FirstAsync(q => q.Id == quizId);

        await _auditLog.LogAsync(
            AuditAction.QUIZ_LINKED_TO_MODULE,
            "Quiz",
            quizId.ToString(),
            $"Quiz '{linkedQuiz.Title}' was assigned to module '{module.Title}'.",
            additionalData: $"ModuleId: {moduleId}",
            userId: userId);

        return new ModuleQuizResponseDto
        {
            Id = existing?.Id ?? 0,
            ModuleId = moduleId,
            QuizId = quizId,
            QuizTitle = linkedQuiz.Title,
            AssignedBy = userId,
            AssignedAt = DateTime.UtcNow,
            Active = true,
        };
    }

    public async Task UnlinkFromModuleAsync(int moduleId)
    {
        var link = await _context.ModuleQuizzes
            .FirstOrDefaultAsync(mq => mq.ModuleId == moduleId)
            ?? throw new KeyNotFoundException("No quiz linked to this module.");

        _context.ModuleQuizzes.Remove(link);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.QUIZ_UNLINKED_FROM_MODULE,
            "Quiz",
            link.QuizId.ToString(),
            "Quiz was unlinked from a module.",
            additionalData: $"ModuleId: {moduleId}");
    }

    private Task LogQuizAsync(
        AuditAction action, Entities.Quiz quiz, string userId, string description,
        string? oldValue = null, string? newValue = null) =>
        _auditLog.LogAsync(
            action,
            "Quiz",
            quiz.Id.ToString(),
            description,
            oldValue: oldValue,
            newValue: newValue,
            additionalData: $"QuestionBankId: {quiz.QuestionBankId}",
            userId: userId);

    public async Task<ModuleQuizResponseDto?> GetModuleQuizAsync(int moduleId)
    {
        return await _context.ModuleQuizzes
            .Where(mq => mq.ModuleId == moduleId)
            .Include(mq => mq.Quiz)
            .Select(mq => new ModuleQuizResponseDto
            {
                Id = mq.Id,
                ModuleId = mq.ModuleId,
                QuizId = mq.QuizId,
                QuizTitle = mq.Quiz.Title,
                AssignedBy = mq.AssignedBy,
                AssignedAt = mq.AssignedAt,
                Active = mq.Active,
            })
            .FirstOrDefaultAsync();
    }

    private async Task<bool> IsAdminAsync(string userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .AnyAsync(name => name == "Admin");
    }

    private async Task<Entities.Quiz?> GetOwnedQuizAsync(int id, string userId, bool includeModuleQuizzes = false)
    {
        var query = _context.Quizzes
            .Include(q => q.QuestionBank)
            .AsQueryable();

        if (includeModuleQuizzes)
            query = query.Include(q => q.ModuleQuizzes);

        var quiz = await query.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz is null) return null;

        if (!await IsAdminAsync(userId) && quiz.CreatedById != userId)
            throw new UnauthorizedAccessException("You do not own this quiz.");

        return quiz;
    }

    private static QuizResponseDto MapToDto(Entities.Quiz q) => new()
    {
        Id = q.Id,
        Title = q.Title,
        QuestionBankId = q.QuestionBankId,
        QuestionBankTitle = q.QuestionBank?.Title ?? "",
        QuestionBankVersion = q.QuestionBank?.Version ?? 0,
        QuestionCount = q.QuestionCount,
        DifficultyFilter = q.DifficultyFilter,
        SelectionMode = q.SelectionMode,
        TimeLimitMinutes = q.TimeLimitMinutes,
        PassingPercentage = q.PassingPercentage,
        MaximumAttempts = q.MaximumAttempts,
        Status = q.Status,
        CreatedById = q.CreatedById,
        CreatedAt = q.CreatedAt,
        PublishedAt = q.PublishedAt,
        ArchivedAt = q.ArchivedAt,
    };
}
