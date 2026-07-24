using LearnPath.API.Common;
using LearnPath.API.DTOs.QuestionBank;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class QuestionBankController : ControllerBase
{
    private readonly IQuestionBankService _service;

    public QuestionBankController(IQuestionBankService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [Authorize(Roles = "Admin")]
    [HttpPost("questionbanks/upload")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        using var stream = file.OpenReadStream();
        var result = await _service.UploadAsync(UserId, file.FileName, stream);

        if (!result.Success)
        {
            var errorMessages = result.Errors.Select(e =>
                $"Question {e.QuestionIndex}: {e.Reason}{(e.Detail is not null ? $" ({e.Detail})" : "")}").ToList();
            return BadRequest(ApiResponse<QuestionBankUploadResult>.Fail(result.Message ?? "Validation failed.", errorMessages));
        }

        return Ok(ApiResponse<QuestionBankUploadResult>.Ok(result, result.Message!));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("questionbanks")]
    public async Task<IActionResult> Search([FromQuery] string? title, [FromQuery] string? subject, [FromQuery] string? tag)
    {
        var results = await _service.SearchAsync(title, subject, tag);
        return Ok(ApiResponse<List<QuestionBankResponseDto>>.Ok(results));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("questionbanks/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Question Bank not found."));

        return Ok(ApiResponse<QuestionBankResponseDto>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("questionbanks/{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var json = await _service.GetStoredJsonAsync(id);
        if (json is null)
            return NotFound(ApiResponse<object>.Fail("Question Bank not found."));

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"questionbank-{id}.json");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("questionbanks/{id:int}/version")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadVersion(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        using var stream = file.OpenReadStream();
        var result = await _service.UploadVersionAsync(id, UserId, file.FileName, stream);

        if (!result.Success)
        {
            var errorMessages = result.Errors.Select(e =>
                $"Question {e.QuestionIndex}: {e.Reason}{(e.Detail is not null ? $" ({e.Detail})" : "")}").ToList();
            return BadRequest(ApiResponse<QuestionBankUploadResult>.Fail(result.Message ?? "Validation failed.", errorMessages));
        }

        return Ok(ApiResponse<QuestionBankUploadResult>.Ok(result, result.Message!));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("questionbanks/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (result is null)
                return NotFound(ApiResponse<object>.Fail("Question Bank not found."));

            return Ok(ApiResponse<QuestionBankResponseDto>.Ok(result, "Question Bank deleted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("questionbanks/{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var result = await _service.ArchiveAsync(id);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Question Bank not found."));

        return Ok(ApiResponse<QuestionBankResponseDto>.Ok(result, "Question Bank archived."));
    }
}
