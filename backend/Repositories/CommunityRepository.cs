using LearnPath.API.Data;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Repositories;

public class CommunityRepository : GenericRepository<Post>, ICommunityRepository
{
    public CommunityRepository(ApplicationDbContext context) : base(context) { }

    // ── Groups ─────────────────────────────────────────────────

    public async Task<Group?> GetGroupByIdAsync(int groupId) =>
        await _context.Groups.FirstOrDefaultAsync(g => g.Id == groupId);

    public async Task<Group?> GetGroupWithMembersAsync(int groupId) =>
        await _context.Groups.AsNoTracking()
            .Include(g => g.Owner)
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Posts)
            .FirstOrDefaultAsync(g => g.Id == groupId);

    public async Task<PagedResult<Group>> SearchGroupsAsync(
        string? search, bool? isPublic, string? ownerId,
        int page, int pageSize)
    {
        var query = _context.Groups.AsNoTracking()
            .Include(g => g.Owner)
            .Include(g => g.Posts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g =>
                g.Name.Contains(search) || g.Description.Contains(search));
        }

        if (isPublic.HasValue)
            query = query.Where(g => g.IsPublic == isPublic.Value);

        if (!string.IsNullOrWhiteSpace(ownerId))
            query = query.Where(g => g.OwnerId == ownerId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Group>(items, total, page, pageSize);
    }

    public async Task<IEnumerable<Group>> GetJoinedGroupsAsync(string userId) =>
        await _context.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Include(m => m.Group)
                .ThenInclude(g => g.Owner)
            .Select(m => m.Group)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Group>> GetOwnedGroupsAsync(string userId) =>
        await _context.Groups.AsNoTracking()
            .Include(g => g.Owner)
            .Where(g => g.OwnerId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<GroupMember>> GetGroupMembersAsync(int groupId) =>
        await _context.GroupMembers.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync();

    public async Task<GroupMember?> GetGroupMemberAsync(int groupId, string userId) =>
        await _context.GroupMembers.AsNoTracking()
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

    public async Task<bool> IsGroupMemberAsync(int groupId, string userId) =>
        await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

    public async Task AddGroupAsync(Group group)
    {
        await _context.Groups.AddAsync(group);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateGroup(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveGroup(Group group)
    {
        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
    }

    public async Task JoinGroupAsync(GroupMember member)
    {
        await _context.GroupMembers.AddAsync(member);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> LeaveGroupAsync(int groupId, string userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member is null) return false;

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Posts ──────────────────────────────────────────────────

    public async Task<Post?> GetPostByIdAsync(int postId) =>
        await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

    public async Task<Post?> GetPostWithDetailsAsync(int postId) =>
        await _context.Posts.AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.LearningPath)
            .Include(p => p.Group)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Votes)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.Author)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.Votes)
            .FirstOrDefaultAsync(p => p.Id == postId);

    public async Task<PagedResult<Post>> SearchPostsAsync(
        string? search, string? category, int? groupId,
        PostSortOrder sort, int page, int pageSize)
    {
        var query = BuildPostQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Title.Contains(search) ||
                p.Content.Contains(search) ||
                (p.Tags != null && p.Tags.Contains(search)) ||
                p.Author.FirstName.Contains(search) ||
                p.Author.LastName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category == category);

        if (groupId.HasValue)
            query = query.Where(p => p.GroupId == groupId.Value);

        return await PaginateAsync(query, sort, page, pageSize);
    }

    public async Task<PagedResult<Post>> GetPostsByGroupAsync(
        int groupId, string? search, string? category,
        PostSortOrder sort, int page, int pageSize)
    {
        var query = BuildPostQuery()
            .Where(p => p.GroupId == groupId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Title.Contains(search) ||
                p.Content.Contains(search) ||
                (p.Tags != null && p.Tags.Contains(search)) ||
                p.Author.FirstName.Contains(search) ||
                p.Author.LastName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category == category);

        return await PaginateAsync(query, sort, page, pageSize);
    }

    public async Task<IEnumerable<Post>> GetPinnedPostsAsync(int? groupId = null) =>
        await _context.Posts.AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.LearningPath)
            .Include(p => p.Group)
            .Include(p => p.Comments)
            .Include(p => p.Votes)
            .Where(p => p.IsPinned && (!groupId.HasValue || p.GroupId == groupId.Value))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task AddPostAsync(Post post)
    {
        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePost(Post post)
    {
        _context.Posts.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task RemovePost(Post post)
    {
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    // ── Comments ───────────────────────────────────────────────

    public async Task<Comment?> GetCommentByIdAsync(int commentId) =>
        await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId);

    public async Task<IEnumerable<Comment>> GetPostCommentsAsync(int postId) =>
        await _context.Comments.AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Votes)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Votes)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Comment>> GetNestedRepliesAsync(int commentId) =>
        await _context.Comments.AsNoTracking()
            .Include(c => c.Author)
            .Include(c => c.Votes)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Votes)
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

    public async Task AddCommentAsync(Comment comment)
    {
        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateComment(Comment comment)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveComment(Comment comment)
    {
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    // ── Votes ──────────────────────────────────────────────────

    public async Task<PostVote?> GetUserPostVoteAsync(string userId, int postId) =>
        await _context.PostVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.PostId == postId);

    public async Task<CommentVote?> GetUserCommentVoteAsync(string userId, int commentId) =>
        await _context.CommentVotes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.CommentId == commentId);

    public async Task AddPostVoteAsync(PostVote vote)
    {
        await _context.PostVotes.AddAsync(vote);
        await _context.SaveChangesAsync();
    }

    public async Task UpdatePostVote(PostVote vote)
    {
        _context.PostVotes.Update(vote);
        await _context.SaveChangesAsync();
    }

    public async Task RemovePostVote(PostVote vote)
    {
        _context.PostVotes.Remove(vote);
        await _context.SaveChangesAsync();
    }

    public async Task AddCommentVoteAsync(CommentVote vote)
    {
        await _context.CommentVotes.AddAsync(vote);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCommentVote(CommentVote vote)
    {
        _context.CommentVotes.Update(vote);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveCommentVote(CommentVote vote)
    {
        _context.CommentVotes.Remove(vote);
        await _context.SaveChangesAsync();
    }

    // ── Helpers ────────────────────────────────────────────────

    private IQueryable<Post> BuildPostQuery() =>
        _context.Posts.AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.LearningPath)
            .Include(p => p.Group)
            .Include(p => p.Comments)
            .Include(p => p.Votes);

    private async Task<PagedResult<Post>> PaginateAsync(
        IQueryable<Post> query, PostSortOrder sort, int page, int pageSize)
    {
        var total = await query.CountAsync();

        var items = await OrderByPostSort(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Post>(items, total, page, pageSize);
    }

    private static IOrderedQueryable<Post> OrderByPostSort(
        IQueryable<Post> query, PostSortOrder sort) =>
        sort switch
        {
            PostSortOrder.Score => query
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.CreatedAt),
            PostSortOrder.Pinned => query
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt),
        };
}
