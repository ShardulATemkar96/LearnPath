using LearnPath.API.DTOs.Attempt;

namespace LearnPath.API.Interfaces.Services;

public interface IAttemptService
{
    Task<AttemptStartResponseDto> StartAttemptAsync(int quizId, int moduleId, string userId);
    Task<AttemptStartResponseDto> GetAttemptAsync(int attemptId, string userId);
    Task SaveAnswerAsync(int attemptId, SaveAnswerRequestDto dto, string userId);
    Task<SubmitResponseDto> SubmitAttemptAsync(int attemptId, string userId);
    Task<ReviewResponseDto> GetReviewAsync(int attemptId, string userId);
}
