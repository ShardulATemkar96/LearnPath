using LearnPath.API.DTOs.Dashboard;

namespace LearnPath.API.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardDataAsync(string userId);
}
