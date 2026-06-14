using LearnPath.API.Algorithms.Graph;
using LearnPath.API.Data;
using LearnPath.API.DTOs.LearningPath;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.LearningPath;

public class LearningPathService : ILearningPathService
{
    private readonly ApplicationDbContext _context;

    public LearningPathService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Paths ─────────────────────────────────────────────────

    public async Task<List<LearningPathResponseDto>> GetAllPublicAsync()
    {
        return await _context.LearningPaths
            .Where(p => p.IsPublished && p.IsPublic)
            .Include(p => p.CreatedBy)
            .Include(p => p.Modules)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<List<LearningPathResponseDto>> GetMyPathsAsync(string userId)
    {
        return await _context.LearningPaths
            .Where(p => p.CreatedById == userId)
            .Include(p => p.CreatedBy)
            .Include(p => p.Modules)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<LearningPathDetailResponseDto> GetByIdAsync(int id, string userId)
    {
        var path = await _context.LearningPaths
            .Include(p => p.CreatedBy)
            .Include(p => p.Modules)
                .ThenInclude(m => m.Dependencies)
            .Include(p => p.Modules)
                .ThenInclude(m => m.Progresses.Where(pr => pr.UserId == userId))
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Learning path not found.");

        // Build completed & unlocked sets
        var completedModuleIds = path.Modules
            .Where(m => m.Progresses.Any(p => p.UserId == userId && p.IsCompleted))
            .Select(m => m.Id)
            .ToHashSet();

        var modules = path.Modules.Select(m =>
        {
            var dependencyIds = m.Dependencies.Select(d => d.DependsOnModuleId).ToList();
            var isUnlocked = dependencyIds.All(dId => completedModuleIds.Contains(dId));
            return new ModuleResponseDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                ContentUrl = m.ContentUrl,
                ContentType = m.ContentType,
                Order = m.Order,
                LearningPathId = m.LearningPathId,
                IsCompleted = completedModuleIds.Contains(m.Id),
                IsUnlocked = isUnlocked,
            };
        }).OrderBy(m => m.Order).ToList();

        var dependencies = path.Modules
            .SelectMany(m => m.Dependencies)
            .Select(d => new ModuleDependencyResponseDto
            {
                ModuleId = d.ModuleId,
                DependsOnModuleId = d.DependsOnModuleId,
            }).ToList();

        return new LearningPathDetailResponseDto
        {
            Id = path.Id,
            Title = path.Title,
            Description = path.Description,
            ThumbnailUrl = path.ThumbnailUrl,
            IsPublished = path.IsPublished,
            IsPublic = path.IsPublic,
            CreatedById = path.CreatedById,
            CreatedByName = $"{path.CreatedBy.FirstName} {path.CreatedBy.LastName}",
            TotalModules = path.Modules.Count,
            CreatedAt = path.CreatedAt,
            UpdatedAt = path.UpdatedAt,
            Modules = modules,
            Dependencies = dependencies,
        };
    }

    public async Task<LearningPathResponseDto> CreateAsync(CreateLearningPathDto dto, string userId)
    {
        var path = new Entities.LearningPath
        {
            Title = dto.Title,
            Description = dto.Description,
            ThumbnailUrl = dto.ThumbnailUrl,
            IsPublic = dto.IsPublic,
            CreatedById = userId,
        };

        await _context.LearningPaths.AddAsync(path);
        await _context.SaveChangesAsync();

        await _context.Entry(path).Reference(p => p.CreatedBy).LoadAsync();
        return MapToResponse(path);
    }

    public async Task<LearningPathResponseDto> UpdateAsync(int id, UpdateLearningPathDto dto, string userId)
    {
        var path = await GetOwnedPathAsync(id, userId);

        path.Title = dto.Title;
        path.Description = dto.Description;
        path.ThumbnailUrl = dto.ThumbnailUrl;
        path.IsPublic = dto.IsPublic;
        path.IsPublished = dto.IsPublished;
        path.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(path);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var path = await GetOwnedPathAsync(id, userId);
        _context.LearningPaths.Remove(path);
        await _context.SaveChangesAsync();
    }

    // ── Modules ───────────────────────────────────────────────

    public async Task<ModuleResponseDto> AddModuleAsync(int pathId, CreateModuleDto dto, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);

        var module = new Entities.Module
        {
            Title = dto.Title,
            Description = dto.Description,
            ContentUrl = dto.ContentUrl,
            ContentType = dto.ContentType,
            Order = dto.Order,
            LearningPathId = pathId,
        };

        await _context.Modules.AddAsync(module);
        await _context.SaveChangesAsync();

        return new ModuleResponseDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            ContentUrl = module.ContentUrl,
            ContentType = module.ContentType,
            Order = module.Order,
            LearningPathId = module.LearningPathId,
            IsCompleted = false,
            IsUnlocked = true,
        };
    }

    public async Task<ModuleResponseDto> UpdateModuleAsync(
        int pathId, int moduleId, UpdateModuleDto dto, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);

        module.Title = dto.Title;
        module.Description = dto.Description;
        module.ContentUrl = dto.ContentUrl;
        module.ContentType = dto.ContentType;
        module.Order = dto.Order;
        module.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ModuleResponseDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            ContentUrl = module.ContentUrl,
            ContentType = module.ContentType,
            Order = module.Order,
            LearningPathId = module.LearningPathId,
            IsCompleted = false,
            IsUnlocked = true,
        };
    }

    public async Task DeleteModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);
        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();
    }

    // ── Dependencies ──────────────────────────────────────────

    public async Task AddDependencyAsync(int pathId, AddDependencyDto dto, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);

        var existingDeps = await _context.ModuleDependencies
            .Where(d => _context.Modules
                .Where(m => m.LearningPathId == pathId)
                .Select(m => m.Id)
                .Contains(d.ModuleId))
            .ToListAsync();

        var adjacency = existingDeps
            .GroupBy(d => d.ModuleId)
            .ToDictionary(g => g.Key, g => g.Select(d => d.DependsOnModuleId).ToList());

        if (DagValidator.WouldCreateCycle(adjacency, dto.ModuleId, dto.DependsOnModuleId))
            throw new ArgumentException("Adding this dependency would create a cycle.");

        var already = await _context.ModuleDependencies
            .AnyAsync(d => d.ModuleId == dto.ModuleId && d.DependsOnModuleId == dto.DependsOnModuleId);

        if (already) throw new ArgumentException("Dependency already exists.");

        await _context.ModuleDependencies.AddAsync(new ModuleDependency
        {
            ModuleId = dto.ModuleId,
            DependsOnModuleId = dto.DependsOnModuleId,
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveDependencyAsync(
        int pathId, int moduleId, int dependsOnModuleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);

        var dep = await _context.ModuleDependencies
            .FirstOrDefaultAsync(d =>
                d.ModuleId == moduleId && d.DependsOnModuleId == dependsOnModuleId)
            ?? throw new KeyNotFoundException("Dependency not found.");

        _context.ModuleDependencies.Remove(dep);
        await _context.SaveChangesAsync();
    }

    // ── Private Helpers ───────────────────────────────────────

    private async Task<Entities.LearningPath> GetOwnedPathAsync(int id, string userId)
    {
        var path = await _context.LearningPaths
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Learning path not found.");

        if (path.CreatedById != userId)
            throw new UnauthorizedAccessException("You do not own this learning path.");

        return path;
    }

    private async Task<Entities.Module> GetModuleAsync(int moduleId, int pathId)
    {
        return await _context.Modules
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.LearningPathId == pathId)
            ?? throw new KeyNotFoundException("Module not found.");
    }

    private static LearningPathResponseDto MapToResponse(Entities.LearningPath path) =>
        new()
        {
            Id = path.Id,
            Title = path.Title,
            Description = path.Description,
            ThumbnailUrl = path.ThumbnailUrl,
            IsPublished = path.IsPublished,
            IsPublic = path.IsPublic,
            CreatedById = path.CreatedById,
            CreatedByName = path.CreatedBy is not null
                ? $"{path.CreatedBy.FirstName} {path.CreatedBy.LastName}"
                : string.Empty,
            TotalModules = path.Modules?.Count ?? 0,
            CreatedAt = path.CreatedAt,
            UpdatedAt = path.UpdatedAt,
        };
}'@

Import-File "backend/Controllers/LearningPathController.cs" @'
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
}'@

$ProgramCs = Join-Path $ProjectRoot "backend\Program.cs"

if (Test-Path $ProgramCs) {
    $ProgramContent = Get-Content $ProgramCs -Raw

    if ($ProgramContent -notmatch 'AddScoped<ILearningPathService,\s*LearningPathService>') {

        $ProgramContent = $ProgramContent -replace `
            '(builder\.Services\.[^\r\n;]+;\s*)+$',
            '$0' + [Environment]::NewLine +
            'builder.Services.AddScoped<ILearningPathService, LearningPathService>();' +
            [Environment]::NewLine

        Set-Content $ProgramCs $ProgramContent -Encoding UTF8
    }
}
else {
    $Missing += "backend/Program.cs"
}

Write-Host ""
Write-Host "======================" -ForegroundColor Cyan
Write-Host "Files Imported : $($Imported.Count)"
Write-Host "Files Skipped  : $($Skipped.Count)"
Write-Host "Files Missing  : $($Missing.Count)"
Write-Host "======================" -ForegroundColor Cyan

if ($Missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing Files:" -ForegroundColor Yellow
    $Missing | ForEach-Object { Write-Host " - $_" }
}
else {
    Write-Host ""
    Write-Host "Import completed successfully." -ForegroundColor Green
}