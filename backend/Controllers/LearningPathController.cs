using LearnPath.API.Common;
using LearnPath.API.DTOs.LearningPath;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/paths")]
[Authorize]
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
    public async Task<IActionResult> GetById(int id, [FromQuery] bool includeUnpublished = false)
    {
        var result = await _service.GetByIdAsync(id, UserId, includeUnpublished);
        return Ok(ApiResponse<LearningPathDetailResponseDto>.Ok(result));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLearningPathDto dto)
    {
        var result = await _service.CreateAsync(dto, UserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<LearningPathResponseDto>.Ok(result, "Learning path created."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLearningPathDto dto)
    {
        var result = await _service.UpdateAsync(id, dto, UserId);
        return Ok(ApiResponse<LearningPathResponseDto>.Ok(result, "Learning path updated."));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Learning path deleted."));
    }

    // ── Modules ───────────────────────────────────────────────

    [HttpGet("{pathId:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> GetModuleContent(int pathId, int moduleId)
    {
        try
        {
            var result = await _service.GetModuleContentAsync(pathId, moduleId, UserId);
            return Ok(ApiResponse<ModuleResponseDto>.Ok(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{pathId:int}/modules")]
    public async Task<IActionResult> AddModule(int pathId, [FromBody] CreateModuleDto dto)
    {
        var result = await _service.AddModuleAsync(pathId, dto, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module added."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{pathId:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> UpdateModule(
        int pathId, int moduleId, [FromBody] UpdateModuleDto dto)
    {
        var result = await _service.UpdateModuleAsync(pathId, moduleId, dto, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module updated."));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{pathId:int}/modules/{moduleId:int}")]
    public async Task<IActionResult> DeleteModule(int pathId, int moduleId)
    {
        await _service.DeleteModuleAsync(pathId, moduleId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Module deleted."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{pathId:int}/modules/{moduleId:int}/archive")]
    public async Task<IActionResult> ArchiveModule(int pathId, int moduleId)
    {
        var result = await _service.ArchiveModuleAsync(pathId, moduleId, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module archived."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{pathId:int}/modules/{moduleId:int}/unarchive")]
    public async Task<IActionResult> UnarchiveModule(int pathId, int moduleId)
    {
        var result = await _service.UnarchiveModuleAsync(pathId, moduleId, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module unarchived."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{pathId:int}/modules/{moduleId:int}/publish")]
    public async Task<IActionResult> PublishModule(int pathId, int moduleId)
    {
        var result = await _service.PublishModuleAsync(pathId, moduleId, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module published."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{pathId:int}/modules/{moduleId:int}/unpublish")]
    public async Task<IActionResult> UnpublishModule(int pathId, int moduleId)
    {
        var result = await _service.UnpublishModuleAsync(pathId, moduleId, UserId);
        return Ok(ApiResponse<ModuleResponseDto>.Ok(result, "Module unpublished."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpGet("{pathId:int}/modules/search")]
    public async Task<IActionResult> SearchModules(
        int pathId,
        [FromQuery] string? search,
        [FromQuery] string? contentType,
        [FromQuery] int? difficulty,
        [FromQuery] bool? isDraft,
        [FromQuery] bool? isArchived)
    {
        var result = await _service.SearchModulesAsync(pathId, search, contentType, difficulty, isDraft, isArchived, UserId);
        return Ok(ApiResponse<List<ModuleResponseDto>>.Ok(result));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPut("{pathId:int}/modules/{moduleId:int}/reorder")]
    public async Task<IActionResult> ReorderModule(int pathId, int moduleId, [FromQuery] bool moveUp)
    {
        await _service.ReorderModuleAsync(pathId, moduleId, moveUp, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Module reordered."));
    }

    // ── Dependencies ──────────────────────────────────────────

    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("{pathId:int}/dependencies")]
    public async Task<IActionResult> AddDependency(
        int pathId, [FromBody] AddDependencyDto dto)
    {
        await _service.AddDependencyAsync(pathId, dto, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Dependency added."));
    }

    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{pathId:int}/dependencies/{moduleId:int}/{dependsOnModuleId:int}")]
    public async Task<IActionResult> RemoveDependency(
        int pathId, int moduleId, int dependsOnModuleId)
    {
        await _service.RemoveDependencyAsync(pathId, moduleId, dependsOnModuleId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Dependency removed."));
    }
}