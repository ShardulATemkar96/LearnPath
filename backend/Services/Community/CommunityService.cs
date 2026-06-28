using LearnPath.API.Data;
using LearnPath.API.DTOs.Community;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Community;

public class CommunityService : ICommunityService
{
    private readonly ApplicationDbContext _context;

    public CommunityService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Posts ─────────────────────────────────────────────────

    public async Task<PostListResponseDto> GetPostsAsync(
        string? category, string? search,
        int page, int pageSize, string userId)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Votes)
            .Include(p => p.LearningPath)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Title.Contains(search) ||
                p.Content.Contains(search));

        var total = await query.CountAsync();

        var posts = await query
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PostListResponseDto
        {
            Posts = posts.Select(p => MapToSummary(p, userId)).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
        };
    }

    public async Task<PostDetailDto> GetPostByIdAsync(int postId, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Votes)
            .Include(p => p.LearningPath)
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
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        post.ViewCount++;
        await _context.SaveChangesAsync();

        var summary = MapToSummary(post, userId);

        var topLevel = post.Comments
            .Where(c => c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => MapComment(c, userId))
            .ToList();

        return new PostDetailDto
        {
            Id                = summary.Id,
            Title             = summary.Title,
            ContentPreview    = summary.ContentPreview,
            Content           = post.Content,
            AuthorId          = summary.AuthorId,
            AuthorName        = summary.AuthorName,
            Category          = summary.Category,
            IsPinned          = summary.IsPinned,
            IsLocked          = summary.IsLocked,
            ViewCount         = post.ViewCount,
            UpvoteCount       = summary.UpvoteCount,
            CommentCount      = summary.CommentCount,
            UserVote          = summary.UserVote,
            LearningPathTitle = summary.LearningPathTitle,
            CreatedAt         = summary.CreatedAt,
            Comments          = topLevel,
        };
    }

    public async Task<PostSummaryDto> CreatePostAsync(
        CreatePostDto dto, string userId)
    {
        var post = new Post
        {
            Title          = dto.Title,
            Content        = dto.Content,
            Category       = dto.Category,
            LearningPathId = dto.LearningPathId,
            AuthorId       = userId,
        };

        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();

        await _context.Entry(post).Reference(p => p.Author).LoadAsync();
        if (dto.LearningPathId.HasValue)
            await _context.Entry(post).Reference(p => p.LearningPath).LoadAsync();

        return MapToSummary(post, userId);
    }

    public async Task<PostSummaryDto> UpdatePostAsync(
        int postId, UpdatePostDto dto, string userId)
    {
        var post = await GetOwnedPostAsync(postId, userId);
        post.Title     = dto.Title;
        post.Content   = dto.Content;
        post.Category  = dto.Category;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return MapToSummary(post, userId);
    }

    public async Task DeletePostAsync(int postId, string userId)
    {
        var post = await GetOwnedPostOrAdminAsync(postId, userId);
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    public async Task<int> VotePostAsync(int postId, VoteDto dto, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        var existing = post.Votes.FirstOrDefault(v => v.UserId == userId);

        if (existing is not null)
        {
            if (existing.IsUpvote == dto.IsUpvote)
            {
                _context.PostVotes.Remove(existing);
            }
            else
            {
                existing.IsUpvote = dto.IsUpvote;
            }
        }
        else
        {
            await _context.PostVotes.AddAsync(new PostVote
            {
                UserId   = userId,
                PostId   = postId,
                IsUpvote = dto.IsUpvote,
            });
        }

        await _context.SaveChangesAsync();

        return await _context.PostVotes
            .CountAsync(v => v.PostId == postId && v.IsUpvote);
    }

    public async Task<CommentDto> AddCommentAsync(
        int postId, CreateCommentDto dto, string userId)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.IsLocked)
            throw new ArgumentException("This post is locked.");

        if (dto.ParentCommentId.HasValue)
        {
            var parent = await _context.Comments
                .AnyAsync(c => c.Id == dto.ParentCommentId && c.PostId == postId);
            if (!parent) throw new KeyNotFoundException("Parent comment not found.");
        }

        var comment = new Comment
        {
            Content         = dto.Content,
            AuthorId        = userId,
            PostId          = postId,
            ParentCommentId = dto.ParentCommentId,
        };

        await _context.Comments.AddAsync(comment);
        await _context.SaveChangesAsync();

        await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

        return MapComment(comment, userId);
    }

    public async Task<CommentDto> UpdateCommentAsync(
        int commentId, UpdateCommentDto dto, string userId)
    {
        var comment = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        if (comment.AuthorId != userId)
            throw new UnauthorizedAccessException("Not your comment.");

        comment.Content   = dto.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapComment(comment, userId);
    }

    public async Task DeleteCommentAsync(int commentId, string userId)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        var isAdmin = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId);

        if (comment.AuthorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Not authorized.");

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    public async Task<int> VoteCommentAsync(
        int commentId, VoteDto dto, string userId)
    {
        var comment = await _context.Comments
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        var existing = comment.Votes.FirstOrDefault(v => v.UserId == userId);

        if (existing is not null)
        {
            if (existing.IsUpvote == dto.IsUpvote)
                _context.CommentVotes.Remove(existing);
            else
                existing.IsUpvote = dto.IsUpvote;
        }
        else
        {
            await _context.CommentVotes.AddAsync(new CommentVote
            {
                UserId    = userId,
                CommentId = commentId,
                IsUpvote  = dto.IsUpvote,
            });
        }

        await _context.SaveChangesAsync();

        return await _context.CommentVotes
            .CountAsync(v => v.CommentId == commentId && v.IsUpvote);
    }

    private async Task<Post> GetOwnedPostAsync(int postId, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.AuthorId != userId)
            throw new UnauthorizedAccessException("Not your post.");

        return post;
    }

    private async Task<Post> GetOwnedPostOrAdminAsync(int postId, string userId)
    {
        var post = await _context.Posts
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        var adminRoleId = await _context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var isAdmin = adminRoleId != null && await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRoleId);

        if (post.AuthorId != userId && !isAdmin)
            throw new UnauthorizedAccessException("Not authorized.");

        return post;
    }

    private static PostSummaryDto MapToSummary(Post post, string userId)
    {
        var upvotes = post.Votes.Count(v => v.IsUpvote);
        var userVote = post.Votes.FirstOrDefault(v => v.UserId == userId);

        return new PostSummaryDto
        {
            Id                = post.Id,
            Title             = post.Title,
            ContentPreview    = post.Content.Length > 200
                ? post.Content[..200] + "..."
                : post.Content,
            AuthorId          = post.AuthorId,
            AuthorName        = $"{post.Author.FirstName} {post.Author.LastName}",
            Category          = post.Category,
            IsPinned          = post.IsPinned,
            IsLocked          = post.IsLocked,
            ViewCount         = post.ViewCount,
            UpvoteCount       = upvotes,
            CommentCount      = post.Comments.Count,
            UserVote          = userVote is null ? 0 : userVote.IsUpvote ? 1 : -1,
            LearningPathTitle = post.LearningPath?.Title,
            CreatedAt         = post.CreatedAt,
        };
    }

    private static CommentDto MapComment(Comment comment, string userId)
    {
        var upvotes  = comment.Votes?.Count(v => v.IsUpvote) ?? 0;
        var userVote = comment.Votes?.FirstOrDefault(v => v.UserId == userId);

        return new CommentDto
        {
            Id              = comment.Id,
            Content         = comment.Content,
            AuthorId        = comment.AuthorId,
            AuthorName      = $"{comment.Author.FirstName} {comment.Author.LastName}",
            PostId          = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            UpvoteCount     = upvotes,
            UserVote        = userVote is null ? 0 : userVote.IsUpvote ? 1 : -1,
            Replies         = comment.Replies?
                .Select(r => MapComment(r, userId))
                .ToList() ?? [],
            CreatedAt       = comment.CreatedAt,
            UpdatedAt       = comment.UpdatedAt,
        };
    }
}
