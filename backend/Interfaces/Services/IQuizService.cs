using LearnPath.API.DTOs.Quiz;

namespace LearnPath.API.Interfaces.Services;

public interface IQuizService
{
    Task<List<QuizResponseDto>> GetAllAsync(string userId);
    Task<QuizResponseDto> GetByIdAsync(int id);
    Task<QuizResponseDto> CreateAsync(CreateQuizDto dto, string userId);
    Task<QuizResponseDto> UpdateAsync(int id, UpdateQuizDto dto, string userId);
    Task<QuizResponseDto?> ArchiveAsync(int id, string userId);
    Task<QuizResponseDto?> PublishAsync(int id, string userId);
    Task<QuizResponseDto?> UnpublishAsync(int id, string userId);
    Task<QuizResponseDto?> DeleteAsync(int id, string userId);

    Task<ModuleQuizResponseDto> LinkToModuleAsync(int moduleId, int quizId, string userId);
    Task UnlinkFromModuleAsync(int moduleId);
    Task<ModuleQuizResponseDto?> GetModuleQuizAsync(int moduleId);
}
