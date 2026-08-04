using System.Security.Claims;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Audit;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Audit;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        AuditAction actionType,
        string entityType,
        string entityId,
        string description,
        string? oldValue = null,
        string? newValue = null,
        string? additionalData = null,
        string? userId = null,
        string? username = null,
        string? role = null)
    {
        var actor = ResolveActor(userId, username, role);

        await _context.AuditLogs.AddAsync(new AuditLog(
            actionType,
            entityType,
            entityId,
            description,
            oldValue,
            newValue,
            additionalData,
            actor.UserId,
            actor.Username,
            actor.Role));

        await _context.SaveChangesAsync();
    }

    public async Task<AuditLogListResponseDto> GetMyHistoryAsync(
        string userId, AuditLogQueryDto query)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("A user context is required.");

        var page     = query.Page     < 1 ? 1  : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        // Ownership is enforced here and cannot be widened by any caller.
        var entries = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.UserId == userId);

        entries = ApplyFilters(
            entries, query.ActionType, query.FromDate, query.ToDate, query.Search);

        var total = await entries.CountAsync();

        var items = await ApplySort(entries, newestFirst: true)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AuditLogListResponseDto
        {
            Entries    = items.Select(MapEntry).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
        };
    }

    public List<AuditActionOptionDto> GetActionOptions() =>
        AuditActionCatalog.GetOptions();

    public async Task<bool> CanAccessAdminHistoryAsync(string requestingUserId)
    {
        if (string.IsNullOrWhiteSpace(requestingUserId)) return false;

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == requestingUserId && u.IsSuperAdmin);
    }

    public async Task<List<string>> GetEntityTypesAsync(string requestingUserId)
    {
        await EnsureSuperAdminAsync(requestingUserId);

        return await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<AdminAuditLogListResponseDto> GetAdminHistoryAsync(
        string requestingUserId, AdminAuditLogQueryDto query)
    {
        await EnsureSuperAdminAsync(requestingUserId);

        var page     = query.Page     < 1 ? 1  : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var entries = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.UserId))
            entries = entries.Where(a => a.UserId == query.UserId);

        if (!string.IsNullOrWhiteSpace(query.Role))
            entries = entries.Where(a => a.Role == query.Role);

        if (!string.IsNullOrWhiteSpace(query.EntityType))
            entries = entries.Where(a => a.EntityType == query.EntityType);

        entries = ApplyFilters(
            entries, query.ActionType, query.FromDate, query.ToDate, query.Search);

        var total = await entries.CountAsync();

        var newestFirst = !string.Equals(query.Sort, "oldest", StringComparison.OrdinalIgnoreCase);

        var items = await ApplySort(entries, newestFirst)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var existence = await ResolveEntityExistenceAsync(items);

        return new AdminAuditLogListResponseDto
        {
            Entries = items.Select(a =>
            {
                var basic = MapEntry(a);
                return new AdminAuditLogResponseDto
                {
                    Id             = basic.Id,
                    Timestamp      = basic.Timestamp,
                    ActionType     = basic.ActionType,
                    ActionName     = basic.ActionName,
                    Category       = basic.Category,
                    EntityType     = basic.EntityType,
                    EntityId       = basic.EntityId,
                    Description    = basic.Description,
                    AdditionalData = basic.AdditionalData,
                    UserId         = a.UserId,
                    Username       = a.Username,
                    Role           = a.Role,
                    EntityExists   = existence.TryGetValue(
                        (a.EntityType, a.EntityId ?? string.Empty), out var exists)
                        ? exists
                        : null,
                };
            }).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
        };
    }

    private async Task EnsureSuperAdminAsync(string requestingUserId)
    {
        if (!await CanAccessAdminHistoryAsync(requestingUserId))
            throw new UnauthorizedAccessException(
                "Only the Super Admin can access administration history.");
    }

    private static IQueryable<AuditLog> ApplyFilters(
        IQueryable<AuditLog> entries,
        AuditAction? actionType,
        DateTime? fromDate,
        DateTime? toDate,
        string? search)
    {
        if (actionType.HasValue)
            entries = entries.Where(a => a.ActionType == actionType.Value);

        if (fromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
            entries = entries.Where(a => a.Timestamp >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateTime.SpecifyKind(toDate.Value.Date, DateTimeKind.Utc).AddDays(1);
            entries = entries.Where(a => a.Timestamp < to);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            entries = entries.Where(a => a.Description.Contains(term));
        }

        return entries;
    }

    private static IQueryable<AuditLog> ApplySort(
        IQueryable<AuditLog> entries, bool newestFirst) =>
        newestFirst
            ? entries.OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id)
            : entries.OrderBy(a => a.Timestamp).ThenBy(a => a.Id);

    private static AuditLogResponseDto MapEntry(AuditLog a) => new()
    {
        Id             = a.Id,
        Timestamp      = a.Timestamp,
        ActionType     = a.ActionType,
        ActionName     = AuditActionCatalog.GetActionName(a.ActionType),
        Category       = AuditActionCatalog.GetCategory(a.ActionType),
        EntityType     = a.EntityType,
        EntityId       = a.EntityId,
        Description    = a.Description,
        AdditionalData = a.AdditionalData,
    };

    /// <summary>
    /// Resolves, for the current page only, whether each referenced navigable
    /// entity still exists. Runs one batched query per entity type.
    /// </summary>
    private async Task<Dictionary<(string EntityType, string EntityId), bool>>
        ResolveEntityExistenceAsync(List<AuditLog> items)
    {
        var result = new Dictionary<(string, string), bool>();

        var referenced = items
            .Where(a => !string.IsNullOrWhiteSpace(a.EntityId))
            .Select(a => (a.EntityType, EntityId: a.EntityId!))
            .Distinct()
            .ToList();

        if (referenced.Count == 0) return result;

        foreach (var group in referenced.GroupBy(r => r.EntityType))
        {
            var ids = group.Select(g => g.EntityId).Distinct().ToList();

            if (group.Key == "User")
            {
                var found = await _context.Users
                    .AsNoTracking()
                    .Where(u => ids.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var id in ids)
                    result[(group.Key, id)] = found.Contains(id);

                continue;
            }

            var numericIds = ids
                .Select(id => (Parsed: int.TryParse(id, out var n), Value: n, Raw: id))
                .Where(x => x.Parsed)
                .ToList();

            if (numericIds.Count == 0) continue;

            var lookup = numericIds.Select(x => x.Value).ToList();

            List<int>? existing = group.Key switch
            {
                "LearningPath" => await _context.LearningPaths.AsNoTracking()
                    .Where(e => lookup.Contains(e.Id)).Select(e => e.Id).ToListAsync(),
                "Classroom" => await _context.Classrooms.AsNoTracking()
                    .Where(e => lookup.Contains(e.Id)).Select(e => e.Id).ToListAsync(),
                "Group" => await _context.Groups.AsNoTracking()
                    .Where(e => lookup.Contains(e.Id)).Select(e => e.Id).ToListAsync(),
                "Post" => await _context.Posts.AsNoTracking()
                    .Where(e => lookup.Contains(e.Id)).Select(e => e.Id).ToListAsync(),
                "Quiz" => await _context.Quizzes.AsNoTracking()
                    .Where(e => lookup.Contains(e.Id)).Select(e => e.Id).ToListAsync(),
                _ => null,
            };

            if (existing is null) continue;

            foreach (var entry in numericIds)
                result[(group.Key, entry.Raw)] = existing.Contains(entry.Value);
        }

        return result;
    }

    private (string? UserId, string? Username, string? Role) ResolveActor(
        string? userId, string? username, string? role)
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (string.IsNullOrEmpty(userId))
            userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(username))
            username = principal?.FindFirstValue(ClaimTypes.Email)
                ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(role))
            role = principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();

        return (userId, username, role);
    }
}
