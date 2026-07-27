using LearnPath.API.DTOs.Quiz;

namespace LearnPath.API.Interfaces.Services;

public interface IQuizService
{
    Task<List<QuizResponseDto>> GetAllAsync();
    Task<QuizResponseDto> GetByIdAsync(int id);
    Task<QuizResponseDto> CreateAsync(CreateQuizDto dto);
    Task<QuizResponseDto> UpdateAsync(int id, UpdateQuizDto dto);
    Task<QuizResponseDto?> ArchiveAsync(int id);
    Task<QuizResponseDto?> PublishAsync(int id);
    Task<QuizResponseDto?> UnpublishAsync(int id);
    Task<QuizResponseDto?> DeleteAsync(int id);

    Task<ModuleQuizResponseDto> LinkToModuleAsync(int moduleId, int quizId, string userId);
    Task UnlinkFromModuleAsync(int moduleId);
    Task<ModuleQuizResponseDto?> GetModuleQuizAsync(int moduleId);
}
