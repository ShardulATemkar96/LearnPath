using LearnPath.API.Common;
using LearnPath.API.DTOs.Ai;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Interfaces.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/submissions")]
[Authorize]
public class SubmissionController : ControllerBase
{
    private readonly IClassroomService _classroomService;
    private readonly IAiFeedbackService _aiFeedbackService;

    public SubmissionController(
        IClassroomService classroomService,
        IAiFeedbackService aiFeedbackService)
    {
        _classroomService = classroomService;
        _aiFeedbackService = aiFeedbackService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("{submissionId:int}/file")]
    public async Task<IActionResult> PreviewFile(int submissionId)
    {
        var (stream, fileName, mimeType) = await _classroomService.GetPreviewFileAsync(submissionId, UserId);

        if (stream is null || fileName is null)
            return NotFound(ApiResponse<object>.Fail("File not found."));

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, mimeType ?? "application/octet-stream", fileName);
    }

    [HttpGet("{submissionId:int}/file/download")]
    public async Task<IActionResult> DownloadFile(int submissionId)
    {
        var (stream, fileName, mimeType) = await _classroomService.GetDownloadFileAsync(submissionId, UserId);

        if (stream is null || fileName is null)
            return NotFound(ApiResponse<object>.Fail("File not found."));

        return File(stream, mimeType ?? "application/octet-stream", fileName);
    }

    [HttpPatch("{submissionId:int}/status")]
    public async Task<IActionResult> TransitionStatus(
        int submissionId, [FromBody] TransitionStatusDto dto)
    {
        var result = await _classroomService.TransitionStatusAsync(submissionId, dto, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Status updated."));
    }

    [HttpPost("{submissionId:int}/return")]
    public async Task<IActionResult> ReturnForResubmission(
        int submissionId, [FromBody] ReturnSubmissionDto dto)
    {
        var result = await _classroomService.ReturnForResubmissionAsync(submissionId, dto, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Returned for resubmission."));
    }

    [HttpGet("{submissionId:int}/ai-feedback")]
    public async Task<IActionResult> GetAiFeedback(int submissionId)
    {
        try
        {
            var result = await _aiFeedbackService.GetAsync(submissionId, UserId);
            if (result is null)
                return NotFound(ApiResponse<object>.Fail("No AI feedback found."));
            return Ok(ApiResponse<AiFeedbackResponseDto>.Ok(result, "AI feedback retrieved."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{submissionId:int}/ai-feedback")]
    public async Task<IActionResult> GenerateAiFeedback(int submissionId)
    {
        try
        {
            var result = await _aiFeedbackService.GenerateAsync(submissionId, UserId);
            return Ok(ApiResponse<AiFeedbackResponseDto>.Ok(result, "AI feedback generated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
