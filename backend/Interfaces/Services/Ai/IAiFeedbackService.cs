using LearnPath.API.DTOs.Ai;

namespace LearnPath.API.Interfaces.Services.Ai;

public interface IAiFeedbackService
{
    Task<AiFeedbackResponseDto> GenerateAsync(int submissionId, string userId);
    Task<AiFeedbackResponseDto?> GetAsync(int submissionId, string userId);
}
