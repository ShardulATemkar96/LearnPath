using LearnPath.API.Common;
using LearnPath.API.DTOs.Attempt;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class AttemptController : ControllerBase
{
    private readonly IAttemptService _service;

    public AttemptController(IAttemptService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("quizzes/{quizId:int}/attempt")]
    public async Task<IActionResult> StartAttempt(int quizId, [FromQuery] int moduleId)
    {
        try
        {
            var result = await _service.StartAttemptAsync(quizId, moduleId, UserId);
            return Ok(ApiResponse<AttemptStartResponseDto>.Ok(result, "Attempt started."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("attempts/{attemptId:int}")]
    public async Task<IActionResult> GetAttempt(int attemptId)
    {
        try
        {
            var result = await _service.GetAttemptAsync(attemptId, UserId);
            return Ok(ApiResponse<AttemptStartResponseDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("attempts/{attemptId:int}/answer")]
    public async Task<IActionResult> SaveAnswer(int attemptId, [FromBody] SaveAnswerRequestDto dto)
    {
        try
        {
            await _service.SaveAnswerAsync(attemptId, dto, UserId);
            return Ok(ApiResponse<object>.Ok(null!, "Answer saved."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> SubmitAttempt(int attemptId)
    {
        try
        {
            var result = await _service.SubmitAttemptAsync(attemptId, UserId);
            return Ok(ApiResponse<SubmitResponseDto>.Ok(result, "Attempt submitted and evaluated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("attempts/{attemptId:int}/review")]
    public async Task<IActionResult> GetReview(int attemptId)
    {
        try
        {
            var result = await _service.GetReviewAsync(attemptId, UserId);
            return Ok(ApiResponse<ReviewResponseDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
