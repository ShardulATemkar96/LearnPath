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
            .Include(p => p.Modules)
                .ThenInclude(m => m.Resources)
            .Include(p => p.Modules)
                .ThenInclude(m => m.Objectives)
            .Include(p => p.Modules)
                .ThenInclude(m => m.Tags)
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
                Difficulty = m.Difficulty,
                EstimatedDurationMinutes = m.EstimatedDurationMinutes,
                NotesHtml = m.NotesHtml,
                PdfUrl = m.PdfUrl,
                ThumbnailUrl = m.ThumbnailUrl,
                IsDraft = m.IsDraft,
                IsArchived = m.IsArchived,
                ArchivedAt = m.ArchivedAt,
                QuizEnabled = m.QuizEnabled,
                QuizQuestionCount = m.QuizQuestionCount,
                QuizPassingScore = m.QuizPassingScore,
                QuizTimeLimitMinutes = m.QuizTimeLimitMinutes,
                Resources = m.Resources.OrderBy(r => r.OrderIndex).Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Type = r.Type,
                    Title = r.Title,
                    Url = r.Url,
                    OrderIndex = r.OrderIndex,
                }).ToList(),
                Objectives = m.Objectives.OrderBy(o => o.OrderIndex).Select(o => new ObjectiveDto
                {
                    Id = o.Id,
                    ObjectiveText = o.ObjectiveText,
                    OrderIndex = o.OrderIndex,
                }).ToList(),
                Tags = m.Tags.Select(t => t.TagName).ToList(),
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
            IsPublished = dto.IsPublic,
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
            Difficulty = dto.Difficulty,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            NotesHtml = dto.NotesHtml,
            PdfUrl = dto.PdfUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            IsDraft = dto.IsDraft,
            QuizEnabled = dto.QuizEnabled,
            QuizQuestionCount = dto.QuizQuestionCount,
            QuizPassingScore = dto.QuizPassingScore,
            QuizTimeLimitMinutes = dto.QuizTimeLimitMinutes,
            ContentBody = dto.ContentBody,
        };
        // Resources
        foreach (var r in dto.Resources)
            module.Resources.Add(new ModuleResource
            {
                Type = r.Type,
                Title = r.Title,
                Url = r.Url,
                OrderIndex = r.OrderIndex,
            });

        // Objectives
        foreach (var o in dto.Objectives)
            module.Objectives.Add(new ModuleObjective
            {
                ObjectiveText = o.ObjectiveText,
                OrderIndex = o.OrderIndex,
            });

        // Tags
        foreach (var tag in dto.Tags)
            module.Tags.Add(new ModuleTag { TagName = tag });


        await _context.Modules.AddAsync(module);
        await _context.SaveChangesAsync();

        return MapToModuleResponse(module);
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
        module.Difficulty = dto.Difficulty;
        module.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        module.NotesHtml = dto.NotesHtml;
        module.PdfUrl = dto.PdfUrl;
        module.ThumbnailUrl = dto.ThumbnailUrl;
        module.IsDraft = dto.IsDraft;
        module.QuizEnabled = dto.QuizEnabled;
        module.QuizQuestionCount = dto.QuizQuestionCount;
        module.QuizPassingScore = dto.QuizPassingScore;
        module.QuizTimeLimitMinutes = dto.QuizTimeLimitMinutes;
        module.ContentBody = dto.ContentBody;
        module.UpdatedAt = DateTime.UtcNow;

        // Replace resources
        _context.ModuleResources.RemoveRange(module.Resources);
        module.Resources = dto.Resources.Select(r => new ModuleResource
        {
            Type = r.Type,
            Title = r.Title,
            Url = r.Url,
            OrderIndex = r.OrderIndex,
        }).ToList();

        // Replace objectives
        _context.ModuleObjectives.RemoveRange(module.Objectives);
        module.Objectives = dto.Objectives.Select(o => new ModuleObjective
        {
            ObjectiveText = o.ObjectiveText,
            OrderIndex = o.OrderIndex,
        }).ToList();

        // Replace tags
        _context.ModuleTags.RemoveRange(module.Tags);
        module.Tags = dto.Tags.Select(t => new ModuleTag { TagName = t }).ToList();
        await _context.SaveChangesAsync();

        return MapToModuleResponse(module);
    }

    public async Task DeleteModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);
        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();
    }

    public async Task<ModuleResponseDto> ArchiveModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);
        module.IsArchived = true;
        module.ArchivedAt = DateTime.UtcNow;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToModuleResponse(module);
    }

    public async Task<ModuleResponseDto> UnarchiveModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);
        module.IsArchived = false;
        module.ArchivedAt = null;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToModuleResponse(module);
    }

    public async Task<List<ModuleResponseDto>> SearchModulesAsync(
        int pathId, string? search, string? contentType, int? difficulty,
        bool? isDraft, bool? isArchived, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);

        var query = _context.Modules
            .Include(m => m.Resources)
            .Include(m => m.Objectives)
            .Include(m => m.Tags)
            .Where(m => m.LearningPathId == pathId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search) || m.Description!.Contains(search));
        if (!string.IsNullOrWhiteSpace(contentType))
            query = query.Where(m => m.ContentType == contentType);
        if (difficulty.HasValue)
            query = query.Where(m => (int)m.Difficulty == difficulty.Value);
        if (isDraft.HasValue)
            query = query.Where(m => m.IsDraft == isDraft.Value);
        if (isArchived.HasValue)
            query = query.Where(m => m.IsArchived == isArchived.Value);

        var modules = await query.OrderBy(m => m.Order).ToListAsync();
        return modules.Select(MapToModuleResponse).ToList();
    }

    public async Task ReorderModuleAsync(int pathId, int moduleId, bool moveUp, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);

        var modules = await _context.Modules
            .Where(m => m.LearningPathId == pathId)
            .OrderBy(m => m.Order)
            .ToListAsync();

        var idx = modules.FindIndex(m => m.Id == moduleId);
        if (idx < 0) throw new KeyNotFoundException("Module not found.");

        var swapIdx = moveUp ? idx - 1 : idx + 1;
        if (swapIdx < 0 || swapIdx >= modules.Count)
            throw new InvalidOperationException("Module is already at the " + (moveUp ? "top" : "bottom") + ".");

        (modules[idx].Order, modules[swapIdx].Order) = (modules[swapIdx].Order, modules[idx].Order);
        modules[idx].UpdatedAt = DateTime.UtcNow;
        modules[swapIdx].UpdatedAt = DateTime.UtcNow;

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

    // ── Private Helpers ───────────────────────────────────────                                                                                                                                                                                                               frozeSam

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
             .Include(m => m.Resources)
              .Include(m => m.Objectives)
               .Include(m => m.Tags)
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.LearningPathId == pathId);
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

    private static ModuleResponseDto MapToModuleResponse(Entities.Module m) => new()
    {
        Id = m.Id,
        Title = m.Title,
        Description = m.Description,
        ContentUrl = m.ContentUrl,
        ContentType = m.ContentType,
        ContentBody = m.ContentBody,
        Order = m.Order,
        LearningPathId = m.LearningPathId,
        IsCompleted = false,
        IsUnlocked = true,
        Difficulty = m.Difficulty,
        EstimatedDurationMinutes = m.EstimatedDurationMinutes,
        NotesHtml = m.NotesHtml,
        PdfUrl = m.PdfUrl,
        ThumbnailUrl = m.ThumbnailUrl,
        IsDraft = m.IsDraft,
        IsArchived = m.IsArchived,
        ArchivedAt = m.ArchivedAt,
        QuizEnabled = m.QuizEnabled,
        QuizQuestionCount = m.QuizQuestionCount,
        QuizPassingScore = m.QuizPassingScore,
        QuizTimeLimitMinutes = m.QuizTimeLimitMinutes,
        Resources = m.Resources.OrderBy(r => r.OrderIndex).Select(r => new ResourceDto
        {
            Id = r.Id, Type = r.Type, Title = r.Title, Url = r.Url, OrderIndex = r.OrderIndex,
        }).ToList(),
        Objectives = m.Objectives.OrderBy(o => o.OrderIndex).Select(o => new ObjectiveDto
        {
            Id = o.Id, ObjectiveText = o.ObjectiveText, OrderIndex = o.OrderIndex,
        }).ToList(),
        Tags = m.Tags.Select(t => t.TagName).ToList(),
    };
}