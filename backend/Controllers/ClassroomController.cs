using LearnPath.API.Common;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/classrooms")]
[Authorize]
public class ClassroomController : ControllerBase
{
    private readonly IClassroomService _service;
    public ClassroomController(IClassroomService service) => _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetMy()
    {
        var result = await _service.GetMyClassroomsAsync(UserId);
        return Ok(ApiResponse<List<ClassroomResponseDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id, UserId);
        return Ok(ApiResponse<ClassroomDetailResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassroomDto dto)
    {
        var result = await _service.CreateAsync(dto, UserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ClassroomResponseDto>.Ok(result, "Classroom created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateClassroomDto dto)
    {
        var result = await _service.UpdateAsync(id, dto, UserId);
        return Ok(ApiResponse<ClassroomResponseDto>.Ok(result, "Classroom updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Classroom deleted."));
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinClassroomDto dto)
    {
        await _service.JoinAsync(dto.InviteCode, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Joined successfully."));
    }

    [HttpPost("{id:int}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        await _service.LeaveAsync(id, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Left classroom."));
    }

    [HttpDelete("{id:int}/members/{memberUserId}")]
    public async Task<IActionResult> RemoveMember(int id, string memberUserId)
    {
        await _service.RemoveMemberAsync(id, memberUserId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Member removed."));
    }

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var result = await _service.GetMembersAsync(id, UserId);
        return Ok(ApiResponse<List<ClassroomMemberDto>>.Ok(result));
    }

    [HttpPost("{id:int}/members/{memberUserId}/invalid")]
    public async Task<IActionResult> MarkInvalid(int id, string memberUserId, [FromBody] MarkInvalidDto dto)
    {
        var result = await _service.MarkInvalidAsync(id, memberUserId, dto, UserId);
        return Ok(ApiResponse<ClassroomMemberDto>.Ok(result, "Member marked invalid."));
    }

    [HttpPost("{classroomId:int}/assignments")]
    public async Task<IActionResult> CreateAssignment(
        int classroomId, [FromBody] CreateAssignmentDto dto)
    {
        var result = await _service.CreateAssignmentAsync(classroomId, dto, UserId);
        return Ok(ApiResponse<AssignmentResponseDto>.Ok(result, "Assignment created."));
    }

    [HttpPut("{classroomId:int}/assignments/{assignmentId:int}")]
    public async Task<IActionResult> UpdateAssignment(
        int classroomId, int assignmentId, [FromBody] UpdateAssignmentDto dto)
    {
        var result = await _service.UpdateAssignmentAsync(classroomId, assignmentId, dto, UserId);
        return Ok(ApiResponse<AssignmentResponseDto>.Ok(result, "Assignment updated."));
    }

    [HttpDelete("{classroomId:int}/assignments/{assignmentId:int}")]
    public async Task<IActionResult> DeleteAssignment(int classroomId, int assignmentId)
    {
        await _service.DeleteAssignmentAsync(classroomId, assignmentId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Assignment deleted."));
    }

    [HttpPost("{classroomId:int}/assignments/{assignmentId:int}/submit")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        int classroomId, int assignmentId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file provided."));

        var result = await _service.UploadSubmissionAsync(classroomId, assignmentId, file, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Submission uploaded."));
    }

    [HttpGet("{classroomId:int}/assignments/{assignmentId:int}/submission")]
    public async Task<IActionResult> GetMySubmission(int classroomId, int assignmentId)
    {
        var result = await _service.GetMySubmissionAsync(classroomId, assignmentId, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result));
    }

    [HttpGet("{classroomId:int}/assignments/{assignmentId:int}/submissions")]
    public async Task<IActionResult> GetSubmissions(int classroomId, int assignmentId)
    {
        var result = await _service.GetSubmissionsAsync(classroomId, assignmentId, UserId);
        return Ok(ApiResponse<List<SubmissionResponseDto>>.Ok(result));
    }

    [HttpPut("{classroomId:int}/assignments/{assignmentId:int}/submissions/{submissionId:int}/grade")]
    public async Task<IActionResult> Grade(
        int classroomId, int assignmentId, int submissionId, [FromBody] GradeSubmissionDto dto)
    {
        var result = await _service.GradeSubmissionAsync(
            classroomId, assignmentId, submissionId, dto, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Graded."));
    }

    [HttpPost("{classroomId:int}/assignments/{assignmentId:int}/submissions/{submissionId:int}/publish")]
    public async Task<IActionResult> PublishEvaluation(int classroomId, int assignmentId, int submissionId)
    {
        try
        {
            var result = await _service.PublishEvaluationAsync(classroomId, assignmentId, submissionId, UserId);
            return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Evaluation published."));
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

    [HttpPut("{classroomId:int}/assignments/{assignmentId:int}/submissions/{submissionId:int}/verify")]
    public async Task<IActionResult> Verify(int classroomId, int assignmentId, int submissionId)
    {
        var result = await _service.VerifySubmissionAsync(classroomId, assignmentId, submissionId, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Verified."));
    }

    [HttpPut("{classroomId:int}/assignments/{assignmentId:int}/submissions/{submissionId:int}/complete")]
    public async Task<IActionResult> Complete(int classroomId, int assignmentId, int submissionId)
    {
        var result = await _service.CompleteSubmissionAsync(classroomId, assignmentId, submissionId, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Completed."));
    }

    [HttpDelete("{classroomId:int}/assignments/{assignmentId:int}/submissions/mine")]
    public async Task<IActionResult> DeleteMySubmission(int classroomId, int assignmentId)
    {
        await _service.DeleteSubmissionAsync(classroomId, assignmentId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Submission cancelled."));
    }
}