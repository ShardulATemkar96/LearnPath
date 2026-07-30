using System.Text.Json;
using LearnPath.API.Common;
using LearnPath.API.Data;
using LearnPath.API.DTOs.QuestionBank;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.QuestionBank;

public class QuestionBankService : IQuestionBankService
{
    private readonly ApplicationDbContext _context;
    private readonly IValidationService _validator;

    public QuestionBankService(ApplicationDbContext context, IValidationService validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<QuestionBankUploadResult> UploadAsync(string userId, string fileName, Stream fileStream)
    {
        var fileSize = fileStream.Length;
        string jsonContent;
        using (var reader = new StreamReader(fileStream, leaveOpen: true))
        {
            jsonContent = await reader.ReadToEndAsync();
        }

        var errors = _validator.Validate(fileName, fileSize, jsonContent);
        if (errors.Count > 0)
        {
            return new QuestionBankUploadResult
            {
                Success = false,
                Errors = errors,
                Message = "Validation failed. No data was saved."
            };
        }

        var model = JsonSerializer.Deserialize<QuestionBankUploadModel>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        var version = 1;
        var existing = await _context.QuestionBanks
            .Where(qb => qb.Title == model.Title && qb.Subject == model.Subject)
            .OrderByDescending(qb => qb.Version)
            .FirstOrDefaultAsync();

        if (existing is not null)
            version = existing.Version + 1;

        var bank = new Entities.QuestionBank
        {
            Title = model.Title,
            Subject = model.Subject,
            Version = version,
            OriginalFileName = fileName,
            StoredJson = jsonContent,
            QuestionCount = model.Questions!.Count,
            Status = QuestionBankStatus.Draft,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
        };

        ParseQuestions(model, bank);

        _context.QuestionBanks.Add(bank);
        await _context.SaveChangesAsync();

        return new QuestionBankUploadResult
        {
            Success = true,
            QuestionBank = MapToDto(bank),
            Message = $"Question Bank uploaded successfully (v{version})."
        };
    }

    public async Task<List<QuestionBankResponseDto>> SearchAsync(string? title, string? subject, string? tag)
    {
        var query = _context.QuestionBanks.AsQueryable();

        if (!string.IsNullOrWhiteSpace(title))
            query = query.Where(qb => qb.Title.Contains(title));

        if (!string.IsNullOrWhiteSpace(subject))
            query = query.Where(qb => qb.Subject.Contains(subject));

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(qb => qb.Tags != null && qb.Tags.Contains(tag));

        return await query
            .OrderByDescending(qb => qb.CreatedAt)
            .Select(qb => MapToDto(qb))
            .ToListAsync();
    }

    public async Task<QuestionBankResponseDto?> GetByIdAsync(int id)
    {
        var bank = await _context.QuestionBanks
            .FirstOrDefaultAsync(qb => qb.Id == id);

        return bank is null ? null : MapToDto(bank);
    }

    public async Task<string?> GetStoredJsonAsync(int id)
    {
        var bank = await _context.QuestionBanks
            .Where(qb => qb.Id == id)
            .Select(qb => qb.StoredJson)
            .FirstOrDefaultAsync();

        return bank;
    }

    public async Task<QuestionBankUploadResult> UploadVersionAsync(int id, string userId, string fileName, Stream fileStream)
    {
        var existing = await _context.QuestionBanks
            .FirstOrDefaultAsync(qb => qb.Id == id);

        if (existing is null)
            return new QuestionBankUploadResult
            {
                Success = false,
                Message = "Question Bank not found."
            };

        var fileSize = fileStream.Length;
        string jsonContent;
        using (var reader = new StreamReader(fileStream, leaveOpen: true))
        {
            jsonContent = await reader.ReadToEndAsync();
        }

        var errors = _validator.Validate(fileName, fileSize, jsonContent);
        if (errors.Count > 0)
        {
            return new QuestionBankUploadResult
            {
                Success = false,
                Errors = errors,
                Message = "Validation failed. No data was saved."
            };
        }

        var model = JsonSerializer.Deserialize<QuestionBankUploadModel>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        var newVersion = new Entities.QuestionBank
        {
            Title = existing.Title,
            Subject = existing.Subject,
            Tags = existing.Tags,
            Version = existing.Version + 1,
            OriginalFileName = fileName,
            StoredJson = jsonContent,
            QuestionCount = model.Questions!.Count,
            Status = QuestionBankStatus.Draft,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
        };

        ParseQuestions(model, newVersion);

        _context.QuestionBanks.Add(newVersion);
        await _context.SaveChangesAsync();

        return new QuestionBankUploadResult
        {
            Success = true,
            QuestionBank = MapToDto(newVersion),
            Message = $"New version (v{newVersion.Version}) uploaded successfully."
        };
    }

    public async Task<QuestionBankResponseDto?> RestoreAsync(int id)
    {
        var bank = await _context.QuestionBanks
            .FirstOrDefaultAsync(qb => qb.Id == id);

        if (bank is null) return null;

        if (bank.Status != QuestionBankStatus.Archived)
            throw new InvalidOperationException("Question Bank is already active.");

        bank.Status = QuestionBankStatus.Active;
        bank.ArchivedAt = null;

        await _context.SaveChangesAsync();
        return MapToDto(bank);
    }

    public async Task<QuestionBankResponseDto?> DeleteAsync(int id)
    {
        var bank = await _context.QuestionBanks
            .Include(qb => qb.Quizzes)
            .FirstOrDefaultAsync(qb => qb.Id == id);

        if (bank is null) return null;

        if (bank.Quizzes.Count > 0)
            throw new InvalidOperationException(
                $"Cannot delete Question Bank '{bank.Title}' because it has {bank.Quizzes.Count} associated quiz(zes). Archive or remove them first.");

        _context.QuestionBanks.Remove(bank);
        await _context.SaveChangesAsync();
        return MapToDto(bank);
    }

    public async Task<QuestionBankResponseDto?> ArchiveAsync(int id)
    {
        var bank = await _context.QuestionBanks
            .FirstOrDefaultAsync(qb => qb.Id == id);

        if (bank is null) return null;

        bank.Status = QuestionBankStatus.Archived;
        bank.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(bank);
    }

    private static void ParseQuestions(QuestionBankUploadModel model, Entities.QuestionBank bank)
    {
        foreach (var qModel in model.Questions!)
        {
            var difficulty = qModel.Difficulty.Trim().ToLowerInvariant() switch
            {
                "easy" => Difficulty.Easy,
                "medium" => Difficulty.Medium,
                "hard" => Difficulty.Hard,
                _ => Difficulty.Easy
            };

            var question = new Entities.Question
            {
                QuestionText = qModel.Question.Trim(),
                Difficulty = difficulty,
                Explanation = qModel.Explanation?.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            for (int i = 0; i < qModel.Options!.Count; i++)
            {
                var isCorrect = qModel.Options[i].Trim()
                    .Equals(qModel.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

                question.Options.Add(new Entities.Option
                {
                    OptionText = qModel.Options[i].Trim(),
                    IsCorrect = isCorrect,
                    DisplayOrder = i,
                });
            }

            bank.Questions.Add(question);
        }
    }

    private static QuestionBankResponseDto MapToDto(Entities.QuestionBank qb) => new()
    {
        Id = qb.Id,
        Title = qb.Title,
        Subject = qb.Subject,
        Tags = qb.Tags,
        Version = qb.Version,
        OriginalFileName = qb.OriginalFileName,
        QuestionCount = qb.QuestionCount,
        Status = qb.Status,
        CreatedBy = qb.CreatedBy,
        CreatedAt = qb.CreatedAt,
        ArchivedAt = qb.ArchivedAt,
    };
}
