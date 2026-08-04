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
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<LearningPathService> _logger;

    public LearningPathService(
        ApplicationDbContext context,
        IAuditLogService auditLog,
        ILogger<LearningPathService> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
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

    public async Task<LearningPathDetailResponseDto> GetByIdAsync(int id, string userId, bool includeUnpublished = false)
    {
        var path = await _context.LearningPaths
            .Include(p => p.CreatedBy)
            .Include(p => p.Modules.Where(m => includeUnpublished || m.IsPublished))
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
                IsPublished = m.IsPublished,
                IsArchived = m.IsArchived,
                ArchivedAt = m.ArchivedAt,
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

        await _auditLog.LogAsync(
            AuditAction.LEARNING_PATH_CREATED,
            "LearningPath",
            path.Id.ToString(),
            $"Learning path '{path.Title}' created.",
            newValue: $"Title: {path.Title}");

        return MapToResponse(path);
    }

    public async Task<LearningPathResponseDto> UpdateAsync(int id, UpdateLearningPathDto dto, string userId)
    {
        var path = await GetOwnedPathAsync(id, userId);

        var wasPublished = path.IsPublished;

        path.Title = dto.Title;
        path.Description = dto.Description;
        path.ThumbnailUrl = dto.ThumbnailUrl;
        path.IsPublic = dto.IsPublic;
        path.IsPublished = dto.IsPublished;
        path.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.LEARNING_PATH_UPDATED,
            "LearningPath",
            path.Id.ToString(),
            $"Learning path '{path.Title}' updated.");

        if (dto.IsPublished && !wasPublished)
            await _auditLog.LogAsync(
                AuditAction.LEARNING_PATH_PUBLISHED,
                "LearningPath",
                path.Id.ToString(),
                $"Learning path '{path.Title}' was published.");

        if (!dto.IsPublished && wasPublished)
            await _auditLog.LogAsync(
                AuditAction.LEARNING_PATH_UNPUBLISHED,
                "LearningPath",
                path.Id.ToString(),
                $"Learning path '{path.Title}' was unpublished.");

        return MapToResponse(path);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        // SQL Server has EnableRetryOnFailure() enabled, so a user-initiated
        // transaction must be executed through the context's execution strategy.
        // This makes the whole delete a single retriable unit per EF Core docs.
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var path = await GetOwnedPathAsync(id, userId);

            var classroomCount = await _context.Classrooms.CountAsync(c => c.LearningPathId == id);
            if (classroomCount > 0)
                throw new ArgumentException(
                    $"Cannot delete learning path \"{path.Title}\" because it has {classroomCount} classroom(s). Delete or reassign the classroom(s) first.");

            var certificateCount = await _context.Certificates.CountAsync(c => c.LearningPathId == id);
            if (certificateCount > 0)
                throw new ArgumentException(
                    $"Cannot delete learning path \"{path.Title}\" because {certificateCount} certificate(s) have been issued for it.");

            var moduleIds = await _context.Modules
                .Where(m => m.LearningPathId == id)
                .Select(m => m.Id)
                .ToListAsync();

            // Delete every dependent row explicitly, in foreign-key-safe order,
            // rather than relying on database ON DELETE CASCADE. This keeps the
            // deletion correct regardless of how the live schema is configured.

            // 1. Student answers owned by the path's quiz attempts.
            var attemptIds = await _context.QuizAttempts
                .Where(a => moduleIds.Contains(a.ModuleId))
                .Select(a => a.Id)
                .ToListAsync();

            if (attemptIds.Count > 0)
                _context.StudentAnswers.RemoveRange(await _context.StudentAnswers
                    .Where(sa => attemptIds.Contains(sa.QuizAttemptId))
                    .ToListAsync());

            // 2. Quiz attempts (FK ModuleId is Restrict — never cascade).
            _context.QuizAttempts.RemoveRange(await _context.QuizAttempts
                .Where(a => moduleIds.Contains(a.ModuleId))
                .ToListAsync());

            // 3. Module dependencies, both directions (FKs to Module).
            _context.ModuleDependencies.RemoveRange(await _context.ModuleDependencies
                .Where(d => moduleIds.Contains(d.ModuleId) || moduleIds.Contains(d.DependsOnModuleId))
                .ToListAsync());

            // 4. Module → quiz assignments.
            _context.ModuleQuizzes.RemoveRange(await _context.ModuleQuizzes
                .Where(mq => moduleIds.Contains(mq.ModuleId))
                .ToListAsync());

            // 5. Progress rows for the path's modules.
            _context.Progresses.RemoveRange(await _context.Progresses
                .Where(p => moduleIds.Contains(p.ModuleId))
                .ToListAsync());

            // 6. Module child content.
            _context.ModuleObjectives.RemoveRange(await _context.ModuleObjectives
                .Where(o => moduleIds.Contains(o.ModuleId))
                .ToListAsync());

            _context.ModuleResources.RemoveRange(await _context.ModuleResources
                .Where(r => moduleIds.Contains(r.ModuleId))
                .ToListAsync());

            _context.ModuleTags.RemoveRange(await _context.ModuleTags
                .Where(t => moduleIds.Contains(t.ModuleId))
                .ToListAsync());

            // 7. The modules themselves, then the path.
            _context.Modules.RemoveRange(await _context.Modules
                .Where(m => moduleIds.Contains(m.Id))
                .ToListAsync());

            await _context.SaveChangesAsync();

            _context.LearningPaths.Remove(path);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        });

        try
        {
            await _auditLog.LogAsync(
                AuditAction.LEARNING_PATH_DELETED,
                "LearningPath",
                id.ToString(),
                "Learning path deleted.");
        }
        catch (Exception logEx)
        {
            // Audit logging is best-effort: a failure here must not turn a
            // successful deletion into an error response.
            _logger.LogError(logEx, "Failed to record LEARNING_PATH_DELETED audit entry.");
        }
    }

    // ── Modules ───────────────────────────────────────────────

    public async Task<ModuleResponseDto> GetModuleContentAsync(int pathId, int moduleId, string userId)
    {
        var path = await _context.LearningPaths
            .Include(p => p.Modules.Where(m => m.IsPublished))
                .ThenInclude(m => m.Dependencies)
            .Include(p => p.Modules)
                .ThenInclude(m => m.Progresses.Where(pr => pr.UserId == userId))
            .FirstOrDefaultAsync(p => p.Id == pathId)
            ?? throw new KeyNotFoundException("Learning path not found.");

        var module = path.Modules.FirstOrDefault(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException("Module not found or not published.");

        var completedModuleIds = path.Modules
            .Where(m => m.Progresses.Any(p => p.UserId == userId && p.IsCompleted))
            .Select(m => m.Id)
            .ToHashSet();

        var dependencyIds = module.Dependencies.Select(d => d.DependsOnModuleId).ToList();
        var isUnlocked = dependencyIds.All(dId => completedModuleIds.Contains(dId));
        var isCompleted = completedModuleIds.Contains(module.Id);

        if (!isUnlocked)
            throw new UnauthorizedAccessException("Complete all prerequisite modules first.");

        // Reload with full includes for the response
        var fullModule = await _context.Modules
            .Include(m => m.Resources)
            .Include(m => m.Objectives)
            .Include(m => m.Tags)
            .FirstAsync(m => m.Id == moduleId);

        var dto = MapToModuleResponse(fullModule);
        dto.IsCompleted = isCompleted;
        dto.IsUnlocked = true;

        var sortedModules = path.Modules.OrderBy(m => m.Order).ToList();
        var currentIndex = sortedModules.FindIndex(m => m.Id == moduleId);
        if (currentIndex > 0)
            dto.PreviousModuleId = sortedModules[currentIndex - 1].Id;
        if (currentIndex < sortedModules.Count - 1)
            dto.NextModuleId = sortedModules[currentIndex + 1].Id;

        return dto;
    }

    public async Task<ModuleResponseDto> AddModuleAsync(int pathId, CreateModuleDto dto, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);

        var titleExists = await _context.Modules
            .AnyAsync(m => m.LearningPathId == pathId && m.Title == dto.Title);
        if (titleExists)
            throw new ArgumentException("A module with this title already exists in this learning path.");

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

        await _auditLog.LogAsync(
            AuditAction.MODULE_CREATED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' created in learning path {pathId}.",
            additionalData: $"PathId: {pathId}");

        return MapToModuleResponse(module);
    }

    public async Task<ModuleResponseDto> UpdateModuleAsync(
        int pathId, int moduleId, UpdateModuleDto dto, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);

        var titleExists = await _context.Modules
            .AnyAsync(m => m.LearningPathId == pathId && m.Title == dto.Title && m.Id != moduleId);
        if (titleExists)
            throw new ArgumentException("A module with this title already exists in this learning path.");

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

        await _auditLog.LogAsync(
            AuditAction.MODULE_UPDATED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' updated in learning path {pathId}.",
            additionalData: $"PathId: {pathId}");

        return MapToModuleResponse(module);
    }

    public async Task DeleteModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);

        var title = module.Title;

        var dependents = await _context.ModuleDependencies
            .Where(d => d.DependsOnModuleId == moduleId)
            .Select(d => d.Module.Title)
            .ToListAsync();

        if (dependents.Count != 0)
            throw new ArgumentException(
                $"Cannot delete module \"{module.Title}\". The following modules depend on it: {string.Join(", ", dependents)}. Remove or reassign dependencies first.");

        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.MODULE_DELETED,
            "Module",
            moduleId.ToString(),
            $"Module '{title}' deleted from learning path {pathId}.",
            additionalData: $"PathId: {pathId}");
    }

    public async Task<ModuleResponseDto> PublishModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);

        if (module.IsArchived)
            throw new InvalidOperationException("Cannot publish an archived module.");

        if (string.IsNullOrWhiteSpace(module.Title))
            throw new ArgumentException("Module title is required before publishing.");

        if (string.IsNullOrWhiteSpace(module.Description))
            throw new ArgumentException("Module description is required before publishing.");

        var hasContent = !string.IsNullOrWhiteSpace(module.ContentBody)
                      || !string.IsNullOrWhiteSpace(module.ContentUrl)
                      || !string.IsNullOrWhiteSpace(module.NotesHtml)
                      || module.Resources.Any();
        if (!hasContent)
            throw new ArgumentException("At least one content item (notes, video, resource, or content body) is required before publishing.");

        module.IsDraft = false;
        module.IsPublished = true;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.MODULE_PUBLISHED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' published in learning path {pathId}.",
            additionalData: $"PathId: {pathId}");

        return MapToModuleResponse(module);
    }

    public async Task<ModuleResponseDto> UnpublishModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);

        if (module.IsArchived)
            throw new InvalidOperationException("Cannot unpublish an archived module.");

        module.IsDraft = true;
        module.IsPublished = false;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.MODULE_UNPUBLISHED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' unpublished in learning path {pathId}.",
            additionalData: $"PathId: {pathId}");

        return MapToModuleResponse(module);
    }

    public async Task<ModuleResponseDto> ArchiveModuleAsync(int pathId, int moduleId, string userId)
    {
        await GetOwnedPathAsync(pathId, userId);
        var module = await GetModuleAsync(moduleId, pathId);
        module.IsArchived = true;
        module.ArchivedAt = DateTime.UtcNow;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.MODULE_ARCHIVED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' archived in learning path {pathId}.",
            additionalData: $"PathId: {pathId}");

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

        await _auditLog.LogAsync(
            AuditAction.MODULE_UNARCHIVED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' unarchived in learning path {pathId}.",
            additionalData: $"PathId: {pathId}");

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

        await _auditLog.LogAsync(
            AuditAction.MODULE_REORDERED,
            "Module",
            moduleId.ToString(),
            $"Module '{modules[idx].Title}' reordered in learning path {pathId}.",
            additionalData: $"PathId: {pathId}; MoveUp: {moveUp}");
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

        await _auditLog.LogAsync(
            AuditAction.DEPENDENCY_ADDED,
            "ModuleDependency",
            $"{dto.ModuleId}:{dto.DependsOnModuleId}",
            $"Dependency added: module {dto.ModuleId} now depends on module {dto.DependsOnModuleId}.",
            additionalData: $"PathId: {pathId}");
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

        await _auditLog.LogAsync(
            AuditAction.DEPENDENCY_REMOVED,
            "ModuleDependency",
            $"{moduleId}:{dependsOnModuleId}",
            $"Dependency removed: module {moduleId} no longer depends on module {dependsOnModuleId}.",
            additionalData: $"PathId: {pathId}");
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
        IsPublished = m.IsPublished,
        IsArchived = m.IsArchived,
        ArchivedAt = m.ArchivedAt,
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