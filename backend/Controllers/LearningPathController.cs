using LearnPath.API.Common;
using LearnPath.API.DTOs.LearningPath;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/paths")]
[Authorize(Roles ="Admin")]
public class LearningPathController : ControllerBase
{
    private readonly ILearningPathService _service;

    public LearningPathController(ILearningPathService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Paths ─────────────────────────────────────────────────

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublic()
    {
        var result = await _service.GetAllPublicAsync();
        return Ok(ApiResponse<List<LearningPathResponseDto>>.Ok(result));
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyPaths()
    {
        var result = await _service.GetMyPathsAsync(UserId);
        return Ok(ApiResponse<List<LearningPathResponseDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id, UserId);
        return Ok(ApiResponse<LearningPathDetailResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLearningPathDto dto)
    {
        var result = await _service.CreateAsync(dto, UserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<LearningPathResponseDto>.Ok(result, "Learning path created."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLearningPathDto dto)
    {
        var result = await _service.UpdateAsync(id, dto, UserId);
        return Ok(ApiResponse<LearningPathResponseDto>.Ok(result, "Learning path updated."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Learning path deleted."));
    }

    // ── Modules ───────────────────────────────────────────────

    [HttpPost("{pathId:int}/modules")]
    public async Task<IActionResult> AddModule(int pathId, [FromBody] CreateModuleDto dto)
    {
        var result = await _service.AddModuleAsync(pathId, dto, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module added."));
    }

    [HttpPut("{pathId:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> UpdateModule(
        int pathId, int moduleId, [FromBody] UpdateModuleDto dto)
    {
        var result = await _service.UpdateModuleAsync(pathId, moduleId, dto, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module updated."));
    }

    [HttpDelete("{pathId:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> DeleteModule(int pathId, int moduleId)
    {
        await _service.DeleteModuleAsync(pathId, moduleId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Module deleted."));
    }

    // ── Dependencies ──────────────────────────────────────────

    [HttpPost("{pathId:int}/dependencies")]
    public async Task<IActionResult> AddDependency(
        int pathId, [FromBody] AddDependencyDto dto)
    {
        await _service.AddDependencyAsync(pathId, dto, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Dependency added."));
    }

    [HttpDelete("{pathId:int}/dependencies/{moduleId:int}/{dependsOnModuleId:int}")]
    public async Task<IActionResult> RemoveDependency(
        int pathId, int moduleId, int dependsOnModuleId)
    {
        await _service.RemoveDependencyAsync(pathId, moduleId, dependsOnModuleId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Dependency removed."));
    }
}