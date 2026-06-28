using LearnPath.API.Common;
using LearnPath.API.DTOs.User;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _service;
    public UserController(IUserService service) => _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _service.GetProfileAsync(UserId);
        return Ok(ApiResponse<UserProfileResponseDto>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var result = await _service.UpdateProfileAsync(UserId, dto);
        return Ok(ApiResponse<UserProfileResponseDto>.Ok(result, "Profile updated."));
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _service.ChangePasswordAsync(UserId, dto);
        return Ok(ApiResponse<object>.Ok(null!, "Password changed."));
    }
}
