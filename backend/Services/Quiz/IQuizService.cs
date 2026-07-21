using LearnPath.API.DTOs.Quiz;

namespace LearnPath.API.Services.Quiz;

public interface IQuizService
{
    // Admin
    Task<QuizResponseDto?> GetQuizByModuleAsync(int moduleId);
    Task<QuizResponseDto> CreateQuizAsync(int moduleId, CreateQuizDto dto);
    Task<QuizResponseDto?> UpdateQuizAsync(int quizId, CreateQuizDto dto);
    Task<bool> DeleteQuizAsync(int quizId);
    Task<AdminQuestionResponseDto> AddQuestionAsync(int quizId, CreateQuestionDto dto);
    Task<AdminQuestionResponseDto?> UpdateQuestionAsync(int questionId, CreateQuestionDto dto);
    Task<bool> DeleteQuestionAsync(int questionId);

    // Learner attempt flow
    Task<StartAttemptResponseDto?> StartAttemptAsync(int quizId);
    Task<AttemptResultDto?> SubmitAttemptAsync(SubmitAnswersDto dto);
    Task<AttemptResultDto?> GetAttemptResultAsync(int attemptId);
    Task<List<AttemptSummaryDto>> GetAttemptHistoryAsync(int quizId);

    // Availability check
    Task<(bool Available, string? Reason)> CheckQuizAvailableAsync(int moduleId);
}
