using LearnPath.API.Data;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Quiz;

public class QuizService : IQuizService
{
    private readonly ApplicationDbContext _context;

    public QuizService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuizResponseDto>> GetAllAsync()
    {
        return await _context.Quizzes
            .Include(q => q.QuestionBank)
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

    public async Task<QuizResponseDto> CreateAsync(CreateQuizDto dto)
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
            CreatedAt = DateTime.UtcNow,
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(quiz.Id);
    }

    public async Task<QuizResponseDto> UpdateAsync(int id, UpdateQuizDto dto)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == id)
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

        quiz.Title = dto.Title;
        quiz.QuestionBankId = dto.QuestionBankId;
        quiz.QuestionCount = dto.QuestionCount;
        quiz.DifficultyFilter = dto.DifficultyFilter;
        quiz.SelectionMode = dto.SelectionMode;
        quiz.TimeLimitMinutes = dto.TimeLimitMinutes;
        quiz.PassingPercentage = dto.PassingPercentage;
        quiz.MaximumAttempts = dto.MaximumAttempts;

        await _context.SaveChangesAsync();
        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> ArchiveAsync(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return null;

        quiz.Status = QuizStatus.Archived;
        quiz.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> PublishAsync(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return null;

        if (quiz.Status == QuizStatus.Archived)
            throw new InvalidOperationException("Cannot publish an archived quiz.");

        quiz.Status = QuizStatus.Published;
        quiz.PublishedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> UnpublishAsync(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return null;

        if (quiz.Status != QuizStatus.Published)
            throw new InvalidOperationException("Only published quizzes can be unpublished.");

        quiz.Status = QuizStatus.Draft;
        quiz.PublishedAt = null;

        await _context.SaveChangesAsync();
        return MapToDto(quiz);
    }

    public async Task<QuizResponseDto?> DeleteAsync(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.ModuleQuizzes)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz is null) return null;

        if (quiz.ModuleQuizzes.Count > 0)
            throw new InvalidOperationException(
                $"Cannot delete quiz '{quiz.Title}' because it is assigned to {quiz.ModuleQuizzes.Count} module(s). Unlink it first.");

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return MapToDto(quiz);
    }

    public async Task<ModuleQuizResponseDto> LinkToModuleAsync(int moduleId, int quizId, string userId)
    {
        var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.Id == quizId)
            ?? throw new KeyNotFoundException("Quiz not found.");

        if (quiz.Status != QuizStatus.Published)
            throw new InvalidOperationException("Quiz must be published before assignment.");

        if (!await _context.Modules.AnyAsync(m => m.Id == moduleId))
            throw new KeyNotFoundException("Module not found.");

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
    }

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
        CreatedAt = q.CreatedAt,
        PublishedAt = q.PublishedAt,
        ArchivedAt = q.ArchivedAt,
    };
}
