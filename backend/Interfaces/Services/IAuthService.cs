using LearnPath.API.DTOs.Auth;

namespace LearnPath.API.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task RevokeTokenAsync(string userId);
}