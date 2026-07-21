using LearnPath.API.Common;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Services.Quiz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/modules/{moduleId}/quiz")]
[Authorize]
public class QuizController : ControllerBase
{
    private readonly IQuizService _service;

    public QuizController(IQuizService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Admin: Quiz CRUD ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetQuiz(int moduleId)
    {
        var result = await _service.GetQuizByModuleAsync(moduleId);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("No quiz configured for this module."));
        return Ok(ApiResponse<QuizResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuiz(int moduleId, [FromBody] CreateQuizDto dto)
    {
        try
        {
            var result = await _service.CreateQuizAsync(moduleId, dto);
            return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz created."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{quizId:int}")]
    public async Task<IActionResult> UpdateQuiz(int moduleId, int quizId, [FromBody] CreateQuizDto dto)
    {
        var result = await _service.UpdateQuizAsync(quizId, dto);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Quiz not found."));
        return Ok(ApiResponse<QuizResponseDto>.Ok(result, "Quiz updated."));
    }

    [HttpDelete("{quizId:int}")]
    public async Task<IActionResult> DeleteQuiz(int moduleId, int quizId)
    {
        var deleted = await _service.DeleteQuizAsync(quizId);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("Quiz not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Quiz deleted."));
    }

    // ── Admin: Question CRUD ───────────────────────────────────

    [HttpPost("{quizId:int}/questions")]
    public async Task<IActionResult> AddQuestion(int moduleId, int quizId, [FromBody] CreateQuestionDto dto)
    {
        try
        {
            var result = await _service.AddQuestionAsync(quizId, dto);
            return Ok(ApiResponse<AdminQuestionResponseDto>.Ok(result, "Question added."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPut("{quizId:int}/questions/{questionId:int}")]
    public async Task<IActionResult> UpdateQuestion(int moduleId, int quizId, int questionId, [FromBody] CreateQuestionDto dto)
    {
        var result = await _service.UpdateQuestionAsync(questionId, dto);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Question not found."));
        return Ok(ApiResponse<AdminQuestionResponseDto>.Ok(result, "Question updated."));
    }

    [HttpDelete("{quizId:int}/questions/{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int moduleId, int quizId, int questionId)
    {
        var deleted = await _service.DeleteQuestionAsync(questionId);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("Question not found."));
        return Ok(ApiResponse<object>.Ok(null!, "Question deleted."));
    }

    // ── Learner: Attempt flow ──────────────────────────────────

    [HttpPost("start")]
    public async Task<IActionResult> StartAttempt(int moduleId)
    {
        var (available, reason) = await _service.CheckQuizAvailableAsync(moduleId);
        if (!available)
            return BadRequest(ApiResponse<object>.Fail(reason!));

        var quiz = await _service.GetQuizByModuleAsync(moduleId);
        if (quiz is null)
            return NotFound(ApiResponse<object>.Fail("Quiz not found."));

        var result = await _service.StartAttemptAsync(quiz.Id);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Failed to start attempt."));
        return Ok(ApiResponse<StartAttemptResponseDto>.Ok(result));
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitAttempt(int moduleId, [FromBody] SubmitAnswersDto dto)
    {
        try
        {
            var result = await _service.SubmitAttemptAsync(dto);
            if (result is null)
                return NotFound(ApiResponse<object>.Fail("Attempt not found."));
            return Ok(ApiResponse<AttemptResultDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("attempts/{attemptId:int}/result")]
    public async Task<IActionResult> GetAttemptResult(int moduleId, int attemptId)
    {
        var result = await _service.GetAttemptResultAsync(attemptId);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("Attempt not found."));
        return Ok(ApiResponse<AttemptResultDto>.Ok(result));
    }

    [HttpGet("attempts")]
    public async Task<IActionResult> GetAttemptHistory(int moduleId)
    {
        var quiz = await _service.GetQuizByModuleAsync(moduleId);
        if (quiz is null)
            return Ok(ApiResponse<List<AttemptSummaryDto>>.Ok([]));

        var history = await _service.GetAttemptHistoryAsync(quiz.Id);
        return Ok(ApiResponse<List<AttemptSummaryDto>>.Ok(history));
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckAvailability(int moduleId)
    {
        var (available, reason) = await _service.CheckQuizAvailableAsync(moduleId);
        return Ok(ApiResponse<object>.Ok(new { available, reason }));
    }
}
