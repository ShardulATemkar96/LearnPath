using LearnPath.API.Algorithms.Graph;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Progress;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Progress;

public class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;

    public ProgressService(
        ApplicationDbContext context,
        IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<ProgressResponseDto> MarkCompleteAsync(
        MarkCompleteDto dto, string userId)
    {
        var module = await _context.Modules
            .Include(m => m.Dependencies)
            .Include(m => m.LearningPath)
            .FirstOrDefaultAsync(m => m.Id == dto.ModuleId)
            ?? throw new KeyNotFoundException("Module not found.");

        // Same unlocking rules as LearningPathService / AttemptService, so
        // completing through this endpoint cannot bypass a locked module.
        var pathModules = await _context.Modules
            .Where(m => m.LearningPathId == module.LearningPathId)
            .Include(m => m.Dependencies)
            .ToListAsync();

        var completedIds = (await _context.Progresses
            .Where(p => p.UserId == userId && p.IsCompleted &&
                        pathModules.Select(pm => pm.Id).Contains(p.ModuleId))
            .Select(p => p.ModuleId)
            .ToListAsync()).ToHashSet();

        var moduleNodes = pathModules.Select(m =>
            ModuleUnlockRules.Module(m.Id, m.Order, m.Dependencies.Select(d => d.DependsOnModuleId)));
        var (orderedNodes, _) = ModuleUnlockRules.OrderModules(moduleNodes);

        if (!ModuleUnlockRules.IsUnlocked(orderedNodes, dto.ModuleId, completedIds))
        {
            var targetNode = orderedNodes.First(n => n.Id == dto.ModuleId);
            throw new ArgumentException(targetNode.PrerequisiteIds.Count > 0
                ? "Complete all prerequisite modules first."
                : "Complete the previous module to unlock this module.");
        }

        var existing = await _context.Progresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == dto.ModuleId);

        if (existing is not null)
        {
            if (existing.IsCompleted)
                throw new ArgumentException("Module already completed.");

            existing.IsCompleted  = true;
            existing.CompletedAt  = DateTime.UtcNow;
            existing.UpdatedAt    = DateTime.UtcNow;
        }
        else
        {
            existing = new Entities.Progress
            {
                UserId      = userId,
                ModuleId    = dto.ModuleId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
            };
            await _context.Progresses.AddAsync(existing);
        }

        await _context.SaveChangesAsync();

        await LogModuleCompletedAsync(module, userId);

        await TryIssueCertificateAsync(module.LearningPathId, userId);

        return new ProgressResponseDto
        {
            Id                 = existing.Id,
            UserId             = existing.UserId,
            ModuleId           = existing.ModuleId,
            ModuleTitle        = module.Title,
            LearningPathId     = module.LearningPathId,
            LearningPathTitle  = module.LearningPath.Title,
            IsCompleted        = existing.IsCompleted,
            CompletedAt        = existing.CompletedAt,
        };
    }

    public async Task MarkModuleCompleteFromQuizAsync(string userId, int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.LearningPath)
            .FirstOrDefaultAsync(m => m.Id == moduleId)
            ?? throw new KeyNotFoundException("Module not found.");

        var existing = await _context.Progresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ModuleId == moduleId);

        if (existing is not null)
        {
            if (existing.IsCompleted) return;

            existing.IsCompleted = true;
            existing.CompletedAt = DateTime.UtcNow;
            existing.UpdatedAt   = DateTime.UtcNow;
        }
        else
        {
            existing = new Entities.Progress
            {
                UserId      = userId,
                ModuleId    = moduleId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
            };
            await _context.Progresses.AddAsync(existing);
        }

        await _context.SaveChangesAsync();

        await LogModuleCompletedAsync(module, userId);

        await TryIssueCertificateAsync(module.LearningPathId, userId);
    }

    private Task LogModuleCompletedAsync(Entities.Module module, string userId) =>
        _auditLog.LogAsync(
            AuditAction.MODULE_COMPLETED,
            "Module",
            module.Id.ToString(),
            $"Module '{module.Title}' completed.",
            additionalData: $"LearningPathId: {module.LearningPathId}",
            userId: userId);

    public async Task<List<PathProgressSummaryDto>> GetUserProgressAsync(string userId)
    {
        return await BuildPathSummariesAsync(
            _context.LearningPaths.Where(p =>
                p.Modules.Any(m => m.Progresses.Any(pr => pr.UserId == userId))),
            userId);
    }

    public async Task<PathProgressSummaryDto> GetPathProgressAsync(
        int pathId, string userId)
    {
        var summaries = await BuildPathSummariesAsync(
            _context.LearningPaths.Where(p => p.Id == pathId), userId);

        return summaries.FirstOrDefault()
            ?? throw new KeyNotFoundException("Learning path not found.");
    }

    // Previously this looped once per enrolled path: one full path load (all
    // module columns) plus one progress query per path. Measured 41 queries
    // and ~86ms for a student enrolled in 20 paths. Rebuilt as three flat
    // queries: matched paths, their modules (completion timestamps inlined as
    // a correlated subquery), and their dependency edges. Summaries are then
    // assembled in memory with the shared ModuleUnlockRules.
    private async Task<List<PathProgressSummaryDto>> BuildPathSummariesAsync(
        IQueryable<Entities.LearningPath> pathQuery, string userId)
    {
        var pathInfos = await pathQuery
            .Select(p => new { p.Id, p.Title })
            .ToListAsync();
        if (pathInfos.Count == 0) return [];

        var pathIds = pathInfos.Select(p => p.Id).ToList();

        var moduleRows = await _context.Modules
            .Where(m => pathIds.Contains(m.LearningPathId))
            .Select(m => new ModuleRow
            {
                Id = m.Id,
                LearningPathId = m.LearningPathId,
                Title = m.Title,
                Order = m.Order,
                IsCompleted = m.Progresses
                    .Any(pr => pr.UserId == userId && pr.IsCompleted),
                CompletedAt = m.Progresses
                    .Where(pr => pr.UserId == userId && pr.IsCompleted)
                    .Select(pr => (DateTime?)pr.CompletedAt)
                    .FirstOrDefault(),
            })
            .ToListAsync();

        var moduleIds = moduleRows.Select(m => m.Id).ToList();

        var dependencyRows = await _context.ModuleDependencies
            .Where(d => moduleIds.Contains(d.ModuleId))
            .Select(d => new { d.ModuleId, d.DependsOnModuleId })
            .ToListAsync();

        var dependenciesByModule = dependencyRows
            .GroupBy(d => d.ModuleId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(d => d.DependsOnModuleId).ToList());

        var summaries = new List<PathProgressSummaryDto>(pathInfos.Count);

        foreach (var path in pathInfos)
        {
            var rows = moduleRows
                .Where(m => m.LearningPathId == path.Id)
                .ToList();

            var rowById = rows.ToDictionary(m => m.Id);
            var moduleNodes = rows.Select(m => ModuleUnlockRules.Module(
                m.Id, m.Order,
                dependenciesByModule.GetValueOrDefault(m.Id, [])));

            var (orderedNodes, _) = ModuleUnlockRules.OrderModules(moduleNodes);
            var completedAtById = rows
                .Where(m => m.IsCompleted)
                .ToDictionary(m => m.Id, m => m.CompletedAt);

            var moduleDtos = orderedNodes.Select(node =>
            {
                var m = rowById[node.Id];
                return new ModuleProgressDto
                {
                    ModuleId    = m.Id,
                    Title       = m.Title,
                    IsCompleted = m.IsCompleted,
                    IsUnlocked  = ModuleUnlockRules.IsUnlocked(
                        orderedNodes, node.Id, completedAtById.Keys.ToHashSet()),
                    CompletedAt = completedAtById.GetValueOrDefault(m.Id),
                };
            }).ToList();

            var completed = moduleDtos.Count(m => m.IsCompleted);
            var total     = moduleDtos.Count;
            var pct       = total > 0 ? (int)Math.Round((double)completed / total * 100) : 0;

            summaries.Add(new PathProgressSummaryDto
            {
                LearningPathId        = path.Id,
                Title                 = path.Title,
                TotalModules          = total,
                CompletedModules      = completed,
                ProgressPercent       = pct,
                IsCertificateEligible = pct == 100,
                Modules               = moduleDtos,
            });
        }

        return summaries;
    }

    private sealed class ModuleRow
    {
        public int Id { get; set; }
        public int LearningPathId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    private async Task TryIssueCertificateAsync(int pathId, string userId)
    {
        var path = await _context.LearningPaths
            .Include(p => p.Modules)
            .FirstOrDefaultAsync(p => p.Id == pathId);

        if (path is null || path.Modules.Count == 0) return;

        var completedCount = await _context.Progresses
            .CountAsync(p => p.UserId == userId && p.IsCompleted &&
                             path.Modules.Select(m => m.Id).Contains(p.ModuleId));

        if (completedCount < path.Modules.Count) return;

        var alreadyIssued = await _context.Certificates
            .AnyAsync(c => c.UserId == userId && c.LearningPathId == pathId);

        if (alreadyIssued) return;

        var certificate = new Entities.Certificate
        {
            UserId         = userId,
            LearningPathId = pathId,
            CertificateUrl = $"/certificates/{userId}/{pathId}",
            IssuedAt       = DateTime.UtcNow,
        };

        await _context.Certificates.AddAsync(certificate);

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CERTIFICATE_GENERATED,
            "Certificate",
            certificate.Id.ToString(),
            $"Certificate generated for learning path '{path.Title}'.",
            additionalData: $"LearningPathId: {pathId}",
            userId: userId);
    }
}
