using LearnPath.API.Common;
using LearnPath.API.DTOs.Analytics;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _service;
    public AnalyticsController(IAnalyticsService service) => _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetMyAnalytics()
    {
        var result = await _service.GetUserAnalyticsAsync(UserId);
        return Ok(ApiResponse<UserAnalyticsResponseDto>.Ok(result));
    }
}