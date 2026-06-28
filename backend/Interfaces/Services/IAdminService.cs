using LearnPath.API.DTOs.Admin;

namespace LearnPath.API.Interfaces.Services;

public interface IAdminService
{
    Task<AdminStatsResponseDto> GetStatsAsync();
    Task<List<AdminUserResponseDto>> GetAllUsersAsync(string? search);
    Task<AdminUserResponseDto> GetUserByIdAsync(string userId);
    Task<AdminUserResponseDto> UpdateUserRoleAsync(string userId, UpdateUserRoleDto dto);
    Task<AdminUserResponseDto> ToggleUserStatusAsync(string userId, ToggleUserStatusDto dto);
    Task DeleteUserAsync(string userId);
}
