using LearnPath.API.Data;
using LearnPath.API.DTOs.Progress;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Progress;

public class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _context;

    public ProgressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProgressResponseDto> MarkCompleteAsync(
        MarkCompleteDto dto, string userId)
    {
        var module = await _context.Modules
            .Include(m => m.Dependencies)
            .Include(m => m.LearningPath)
            .FirstOrDefaultAsync(m => m.Id == dto.ModuleId)
            ?? throw new KeyNotFoundException("Module not found.");

        var depIds = module.Dependencies.Select(d => d.DependsOnModuleId).ToList();
        if (depIds.Any())
        {
            var completedDepCount = await _context.Progresses
                .CountAsync(p =>
                    p.UserId == userId &&
                    p.IsCompleted &&
                    depIds.Contains(p.ModuleId));

            if (completedDepCount < depIds.Count)
                throw new ArgumentException(
                    "Complete all prerequisite modules first.");
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

    public async Task<List<PathProgressSummaryDto>> GetUserProgressAsync(string userId)
    {
        var enrolledPathIds = await _context.Progresses
            .Where(p => p.UserId == userId)
            .Select(p => p.Module.LearningPathId)
            .Distinct()
            .ToListAsync();

        var summaries = new List<PathProgressSummaryDto>();

        foreach (var pathId in enrolledPathIds)
            summaries.Add(await BuildPathSummaryInternalAsync(pathId, userId));

        return summaries;
    }

    public async Task<PathProgressSummaryDto> GetPathProgressAsync(
        int pathId, string userId)
    {
        var exists = await _context.LearningPaths.AnyAsync(p => p.Id == pathId);
        if (!exists) throw new KeyNotFoundException("Learning path not found.");
        return await BuildPathSummaryInternalAsync(pathId, userId);
    }

    private async Task<PathProgressSummaryDto> BuildPathSummaryInternalAsync(
        int pathId, string userId)
    {
        var path = await _context.LearningPaths
            .Include(p => p.Modules)
                .ThenInclude(m => m.Dependencies)
            .FirstOrDefaultAsync(p => p.Id == pathId)
            ?? throw new KeyNotFoundException("Path not found.");

        var completedIds = await _context.Progresses
            .Where(p => p.UserId == userId && p.IsCompleted &&
                        path.Modules.Select(m => m.Id).Contains(p.ModuleId))
            .ToDictionaryAsync(p => p.ModuleId, p => p.CompletedAt);

        var moduleDtos = path.Modules.Select(m =>
        {
            var depIds = m.Dependencies.Select(d => d.DependsOnModuleId).ToList();
            var isUnlocked = depIds.All(d => completedIds.ContainsKey(d));

            return new ModuleProgressDto
            {
                ModuleId    = m.Id,
                Title       = m.Title,
                IsCompleted = completedIds.ContainsKey(m.Id),
                IsUnlocked  = isUnlocked,
                CompletedAt = completedIds.TryGetValue(m.Id, out var dt) ? dt : null,
            };
        }).OrderBy(m => m.ModuleId).ToList();

        var completed = moduleDtos.Count(m => m.IsCompleted);
        var total     = moduleDtos.Count;
        var pct       = total > 0 ? (int)Math.Round((double)completed / total * 100) : 0;

        return new PathProgressSummaryDto
        {
            LearningPathId        = path.Id,
            Title                 = path.Title,
            TotalModules          = total,
            CompletedModules      = completed,
            ProgressPercent       = pct,
            IsCertificateEligible = pct == 100,
            Modules               = moduleDtos,
        };
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

        await _context.Certificates.AddAsync(new Entities.Certificate
        {
            UserId         = userId,
            LearningPathId = pathId,
            CertificateUrl = $"/certificates/{userId}/{pathId}",
            IssuedAt       = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();
    }
}
