using LearnPath.API.DTOs.Analytics;

namespace LearnPath.API.Interfaces.Services;

public interface IAnalyticsService
{
    Task<UserAnalyticsResponseDto> GetUserAnalyticsAsync(string userId);
}
