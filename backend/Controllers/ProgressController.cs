using LearnPath.API.Common;
using LearnPath.API.DTOs.Progress;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/progress")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _service;
    public ProgressController(IProgressService service) => _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("complete")]
    public async Task<IActionResult> MarkComplete([FromBody] MarkCompleteDto dto)
    {
        var result = await _service.MarkCompleteAsync(dto, UserId);
        return Ok(ApiResponse<ProgressResponseDto>.Ok(result, "Module marked complete."));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProgress()
    {
        var result = await _service.GetUserProgressAsync(UserId);
        return Ok(ApiResponse<List<PathProgressSummaryDto>>.Ok(result));
    }

    [HttpGet("paths/{pathId:int}")]
    public async Task<IActionResult> GetPathProgress(int pathId)
    {
        var result = await _service.GetPathProgressAsync(pathId, UserId);
        return Ok(ApiResponse<PathProgressSummaryDto>.Ok(result));
    }
}