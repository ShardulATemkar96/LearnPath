using LearnPath.API.DTOs.LearningPath;

namespace LearnPath.API.Interfaces.Services;

public interface ILearningPathService
{
    Task<List<LearningPathResponseDto>> GetAllPublicAsync();
    Task<List<LearningPathResponseDto>> GetMyPathsAsync(string userId);
    Task<LearningPathDetailResponseDto> GetByIdAsync(int id, string userId);
    Task<LearningPathResponseDto> CreateAsync(CreateLearningPathDto dto, string userId);
    Task<LearningPathResponseDto> UpdateAsync(int id, UpdateLearningPathDto dto, string userId);
    Task DeleteAsync(int id, string userId);

    Task<ModuleResponseDto> AddModuleAsync(int pathId, CreateModuleDto dto, string userId);
    Task<ModuleResponseDto> UpdateModuleAsync(int pathId, int moduleId, UpdateModuleDto dto, string userId);
    Task DeleteModuleAsync(int pathId, int moduleId, string userId);

    Task<ModuleResponseDto> ArchiveModuleAsync(int pathId, int moduleId, string userId);
    Task<ModuleResponseDto> UnarchiveModuleAsync(int pathId, int moduleId, string userId);
    Task<List<ModuleResponseDto>> SearchModulesAsync(int pathId, string? search, string? contentType, int? difficulty, bool? isDraft, bool? isArchived, string userId);
    Task ReorderModuleAsync(int pathId, int moduleId, bool moveUp, string userId);

    Task AddDependencyAsync(int pathId, AddDependencyDto dto, string userId);
    Task RemoveDependencyAsync(int pathId, int moduleId, int dependsOnModuleId, string userId);
}