using LearnPath.API.DTOs.User;

namespace LearnPath.API.Interfaces.Services;

public interface IUserService
{
    Task<UserProfileResponseDto> GetProfileAsync(string userId);
    Task<UserProfileResponseDto> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
}
