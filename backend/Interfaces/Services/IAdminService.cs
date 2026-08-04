using LearnPath.API.DTOs.Admin;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.DTOs.LearningPath;

namespace LearnPath.API.Interfaces.Services;

public interface IAdminService
{
    Task<AdminStatsResponseDto> GetStatsAsync();
    Task<List<AdminUserResponseDto>> GetAllUsersAsync(string? search);
    Task<AdminUserResponseDto> GetUserByIdAsync(string userId);
    Task<AdminUserResponseDto> UpdateUserRoleAsync(string userId, UpdateUserRoleDto dto, string actorId);
    Task<AdminUserResponseDto> ActivateUserAsync(string userId);
    Task<AdminUserResponseDto> DeactivateUserAsync(string userId);
    Task<AdminUserResponseDto> MarkInvalidAsync(string userId, MarkInvalidDto dto, string actorId);
    Task DeleteUserAsync(string userId);
    Task<List<LearningPathResponseDto>> GetAllPathsAsync();
}
