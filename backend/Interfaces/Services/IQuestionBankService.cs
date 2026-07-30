using LearnPath.API.DTOs.QuestionBank;

namespace LearnPath.API.Interfaces.Services;

public interface IQuestionBankService
{
    Task<QuestionBankUploadResult> UploadAsync(string userId, string fileName, Stream fileStream);
    Task<List<QuestionBankResponseDto>> SearchAsync(string? title, string? subject, string? tag);
    Task<QuestionBankResponseDto?> GetByIdAsync(int id);
    Task<string?> GetStoredJsonAsync(int id);
    Task<QuestionBankUploadResult> UploadVersionAsync(int id, string userId, string fileName, Stream fileStream);
    Task<QuestionBankResponseDto?> ArchiveAsync(int id);
    Task<QuestionBankResponseDto?> RestoreAsync(int id);
    Task<QuestionBankResponseDto?> DeleteAsync(int id);
}
