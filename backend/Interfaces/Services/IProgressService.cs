using LearnPath.API.DTOs.Progress;

namespace LearnPath.API.Interfaces.Services;

public interface IProgressService
{
    Task<ProgressResponseDto> MarkCompleteAsync(MarkCompleteDto dto, string userId);
    Task<List<PathProgressSummaryDto>> GetUserProgressAsync(string userId);
    Task<PathProgressSummaryDto> GetPathProgressAsync(int pathId, string userId);
}
