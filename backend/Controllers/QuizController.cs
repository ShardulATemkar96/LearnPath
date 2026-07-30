using LearnPath.API.Common;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly IQuizService _service;

    public QuizController(IQuizService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [Authorize(Roles = "Admin")]
    [HttpGet("quizzes")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<List<QuizResponseDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("quizzes/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<QuizResponseDto>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("quizzes")]
    public async Task<IActionResult> Create([FromBody] CreateQuizDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<QuizResponseDto>.Ok(result, "Quiz created."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("quizzes/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQuizDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("quizzes/{id:int}/archive")]
    public async Task<IActionResult> Archive(int id)
    {
        var result = await _service.ArchiveAsync(id);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Quiz not found."));

        return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz archived."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("quizzes/{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            var result = await _service.PublishAsync(id);
            if (result is null)
                return NotFound(ApiResponse<object>.Fail("Quiz not found."));

            return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz published successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("quizzes/{id:int}/unpublish")]
    public async Task<IActionResult> Unpublish(int id)
    {
        try
        {
            var result = await _service.UnpublishAsync(id);
            if (result is null)
                return NotFound(ApiResponse<object>.Fail("Quiz not found."));

            return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz unpublished."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("quizzes/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);
            if (result is null)
                return NotFound(ApiResponse<object>.Fail("Quiz not found."));

            return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz deleted successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("modules/{moduleId:int}/quiz")]
    public async Task<IActionResult> LinkQuiz(int moduleId, [FromBody] LinkQuizDto dto)
    {
        try
        {
            var result = await _service.LinkToModuleAsync(moduleId, dto.QuizId, UserId);
            return Ok(ApiResponse<ModuleQuizResponseDto>.Ok(result, "Quiz assigned to module."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("modules/{moduleId:int}/quiz")]
    public async Task<IActionResult> UnlinkQuiz(int moduleId)
    {
        try
        {
            await _service.UnlinkFromModuleAsync(moduleId);
            return Ok(ApiResponse<object>.Ok(null!, "Quiz unlinked from module."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("modules/{moduleId:int}/quiz")]
    public async Task<IActionResult> GetModuleQuiz(int moduleId)
    {
        var result = await _service.GetModuleQuizAsync(moduleId);
        if (result is null)
            return Ok(ApiResponse<object>.Ok(null!, "No quiz linked to this module."));

        return Ok(ApiResponse<ModuleQuizResponseDto>.Ok(result));
    }
}
