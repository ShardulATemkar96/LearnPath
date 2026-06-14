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

    [HttpGet("{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        var result = await _service.GetMembersAsync(id, UserId);
        return Ok(ApiResponse<List<ClassroomMemberDto>>.Ok(result));
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
    public async Task<IActionResult> Submit(
        int classroomId, int assignmentId, [FromBody] CreateSubmissionDto dto)
    {
        var result = await _service.SubmitAssignmentAsync(classroomId, assignmentId, dto, UserId);
        return Ok(ApiResponse<SubmissionResponseDto>.Ok(result, "Submitted successfully."));
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
}