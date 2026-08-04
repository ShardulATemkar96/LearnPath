using LearnPath.API.Data;
using LearnPath.API.DTOs.Community;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Repositories;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Community;

public class CommunityService : ICommunityService
{
    private readonly ApplicationDbContext _context;
    private readonly ICommunityRepository _repository;
    private readonly IAuditLogService _auditLog;

    public CommunityService(
        ApplicationDbContext context,
        IAuditLogService auditLog,
        ICommunityRepository? repository = null)
    {
        _context = context;
        _auditLog = auditLog;
        _repository = repository ?? new CommunityRepository(context);
    }

    // ── Posts ─────────────────────────────────────────────────

    public async Task<PostListResponseDto> GetPostsAsync(
        string? category, string? search, string? tag, PostSortOrder sort,
        PostFilter filter, int page, int pageSize, string userId)
    {
        var query = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Group)
            .Include(p => p.Comments)
            .Include(p => p.Votes)
            .Include(p => p.LearningPath)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Title.Contains(search) ||
                p.Content.Contains(search) ||
                (p.Tags != null && p.Tags.Contains(search)) ||
                (p.Author.FirstName + " " + p.Author.LastName).Contains(search));

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(p => p.Tags != null && p.Tags.Contains(tag));

        switch (filter)
        {
            case PostFilter.Mine:
                query = query.Where(p => p.AuthorId == userId);
                break;
            case PostFilter.Pinned:
                query = query.Where(p => p.IsPinned);
                break;
            case PostFilter.WithCode:
                query = query.Where(p => p.CodeSnippet != null);
                break;
            case PostFilter.WithoutCode:
                query = query.Where(p => p.CodeSnippet == null);
                break;
        }

        if (sort == PostSortOrder.Trending)
            return await GetTrendingPostsAsyncInternal(query, page, pageSize, userId);

        var total = await query.CountAsync();

        var ordered = sort == PostSortOrder.Score
            ? query.OrderByDescending(p => p.Score).ThenByDescending(p => p.CreatedAt)
            : query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt);

        var posts = await ordered
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

    private async Task<PostListResponseDto> GetTrendingPostsAsyncInternal(
        IQueryable<Post> query, int page, int pageSize, string userId)
    {
        var total = await query.CountAsync();

        var candidates = await query
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.CreatedAt)
            .Take(Math.Max(pageSize * 10, 100))
            .ToListAsync();

        var ranked = candidates
            .Select(p => (Post: p, Score: CalculateTrendingScore(p.Score, p.CreatedAt)))
            .OrderByDescending(x => x.Score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Post);

        return new PostListResponseDto
        {
            Posts      = ranked.Select(p => MapToSummary(p, userId)).ToList(),
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
            Id                  = summary.Id,
            Title               = summary.Title,
            ContentPreview      = summary.ContentPreview,
            Content             = post.Content,
            CodeSnippet         = post.CodeSnippet,
            ProgrammingLanguage = post.ProgrammingLanguage,
            Tags                = post.Tags,
            AuthorId            = summary.AuthorId,
            AuthorName        = summary.AuthorName,
            Category          = summary.Category,
            IsPinned          = summary.IsPinned,
            IsLocked          = summary.IsLocked,
            ViewCount         = post.ViewCount,
            UpvoteCount       = summary.UpvoteCount,
            CommentCount      = summary.CommentCount,
            UserVote          = summary.UserVote,
            LearningPathTitle = summary.LearningPathTitle,
            Edited            = summary.Edited,
            EditedAt          = summary.EditedAt,
            CreatedAt         = summary.CreatedAt,
            Comments          = topLevel,
        };
    }

    public async Task<PostSummaryDto> CreatePostAsync(
        CreatePostDto dto, string userId)
    {
        var post = new Post
        {
            Title               = dto.Title,
            Content             = dto.Content,
            CodeSnippet         = dto.CodeSnippet,
            ProgrammingLanguage = dto.ProgrammingLanguage,
            Tags                = dto.Tags,
            Category            = dto.Category,
            LearningPathId      = dto.LearningPathId,
            AuthorId            = userId,
        };

        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();

        await LogPostAsync(AuditAction.POST_CREATED, post, userId,
            $"Post '{post.Title}' was created.");

        await _context.Entry(post).Reference(p => p.Author).LoadAsync();
        if (dto.LearningPathId.HasValue)
            await _context.Entry(post).Reference(p => p.LearningPath).LoadAsync();

        return MapToSummary(post, userId);
    }

    public async Task<PostSummaryDto> UpdatePostAsync(
        int postId, UpdatePostDto dto, string userId)
    {
        var post = await GetOwnedPostAsync(postId, userId);
        var oldValue             = $"Title: {post.Title}; Category: {post.Category}";
        post.Title               = dto.Title;
        post.Content             = dto.Content;
        post.CodeSnippet         = dto.CodeSnippet;
        post.ProgrammingLanguage = dto.ProgrammingLanguage;
        post.Tags                = dto.Tags;
        post.Category            = dto.Category;
        post.Edited              = true;
        post.EditedAt            = DateTime.UtcNow;
        post.UpdatedAt           = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogPostAsync(AuditAction.POST_UPDATED, post, userId,
            $"Post '{post.Title}' was updated.",
            oldValue: oldValue,
            newValue: $"Title: {post.Title}; Category: {post.Category}");

        return MapToSummary(post, userId);
    }

    public async Task DeletePostAsync(int postId, string userId)
    {
        var post = await GetOwnedPostOrAdminAsync(postId, userId);
        var title = post.Title;
        var groupId = post.GroupId;
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.POST_DELETED,
            "Post",
            postId.ToString(),
            $"Post '{title}' was deleted.",
            additionalData: groupId.HasValue ? $"GroupId: {groupId}" : null,
            userId: userId);
    }

    public async Task<int> VotePostAsync(int postId, VoteDto dto, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.GroupId.HasValue)
            await EnsureActiveMembershipAsync(post.GroupId.Value, userId);

        var existing = post.Votes.FirstOrDefault(v => v.UserId == userId);

        int scoreDelta;
        bool voteWithdrawn = false;
        if (existing is not null && existing.IsUpvote == dto.IsUpvote)
        {
            _context.PostVotes.Remove(existing);
            scoreDelta = existing.IsUpvote ? -1 : +1;
            voteWithdrawn = true;
        }
        else if (existing is not null)
        {
            scoreDelta = existing.IsUpvote ? -2 : +2;
            existing.IsUpvote = dto.IsUpvote;
        }
        else
        {
            scoreDelta = dto.IsUpvote ? +1 : -1;
            await _context.PostVotes.AddAsync(new PostVote
            {
                UserId   = userId,
                PostId   = postId,
                IsUpvote = dto.IsUpvote,
            });
        }

        post.Score    += scoreDelta;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (voteWithdrawn)
            await LogPostAsync(AuditAction.VOTE_REMOVED, post, userId,
                $"Vote removed from post '{post.Title}'.");
        else
            await LogPostAsync(
                dto.IsUpvote ? AuditAction.POST_UPVOTED : AuditAction.POST_DOWNVOTED,
                post, userId,
                $"Post '{post.Title}' was {(dto.IsUpvote ? "upvoted" : "downvoted")}.");

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

        if (post.GroupId.HasValue)
            await EnsureActiveMembershipAsync(post.GroupId.Value, userId);

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

        await _auditLog.LogAsync(
            AuditAction.COMMENT_CREATED,
            "Comment",
            comment.Id.ToString(),
            $"Comment added to post '{post.Title}'.",
            additionalData: $"PostId: {postId}",
            userId: userId);

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

        var oldContent    = comment.Content;
        comment.Content   = dto.Content;
        comment.Edited    = true;
        comment.EditedAt  = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.COMMENT_UPDATED,
            "Comment",
            comment.Id.ToString(),
            "Comment was updated.",
            oldValue: Truncate(oldContent),
            newValue: Truncate(comment.Content),
            additionalData: $"PostId: {comment.PostId}",
            userId: userId);

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

        var postId = comment.PostId;
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.COMMENT_DELETED,
            "Comment",
            commentId.ToString(),
            "Comment was deleted.",
            additionalData: $"PostId: {postId}",
            userId: userId);
    }

    public async Task<int> VoteCommentAsync(
        int commentId, VoteDto dto, string userId)
    {
        var comment = await _context.Comments
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        await _context.Entry(comment).Reference(c => c.Post).LoadAsync();
        if (comment.Post.GroupId.HasValue)
            await EnsureActiveMembershipAsync(comment.Post.GroupId.Value, userId);

        var existing = comment.Votes.FirstOrDefault(v => v.UserId == userId);

        int scoreDelta;
        if (existing is not null && existing.IsUpvote == dto.IsUpvote)
        {
            _context.CommentVotes.Remove(existing);
            scoreDelta = existing.IsUpvote ? -1 : +1;
        }
        else if (existing is not null)
        {
            scoreDelta = existing.IsUpvote ? -2 : +2;
            existing.IsUpvote = dto.IsUpvote;
        }
        else
        {
            scoreDelta = dto.IsUpvote ? +1 : -1;
            await _context.CommentVotes.AddAsync(new CommentVote
            {
                UserId    = userId,
                CommentId = commentId,
                IsUpvote  = dto.IsUpvote,
            });
        }

        comment.Score += scoreDelta;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await _context.CommentVotes
            .CountAsync(v => v.CommentId == commentId && v.IsUpvote);
    }

    public async Task<List<CommentDto>> GetPostCommentsAsync(
        int postId, string userId)
    {
        var postExists = await _context.Posts
            .AnyAsync(p => p.Id == postId);

        if (!postExists)
            throw new KeyNotFoundException("Post not found.");

        var comments = await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.Votes)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Votes)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(c => MapComment(c, userId)).ToList();
    }

    public async Task RemovePostVoteAsync(int postId, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.GroupId.HasValue)
            await EnsureActiveMembershipAsync(post.GroupId.Value, userId);

        var existing = post.Votes.FirstOrDefault(v => v.UserId == userId);
        if (existing is null)
            return;

        _context.PostVotes.Remove(existing);
        post.Score    += existing.IsUpvote ? -1 : +1;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogPostAsync(AuditAction.VOTE_REMOVED, post, userId,
            $"Vote removed from post '{post.Title}'.");
    }

    public async Task RemoveCommentVoteAsync(int commentId, string userId)
    {
        var comment = await _context.Comments
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        var existing = comment.Votes.FirstOrDefault(v => v.UserId == userId);
        if (existing is null)
            return;

        _context.CommentVotes.Remove(existing);
        comment.Score    += existing.IsUpvote ? -1 : +1;
        comment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
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
            Id                  = post.Id,
            Title               = post.Title,
            ContentPreview      = post.Content.Length > 200
                ? post.Content[..200] + "..."
                : post.Content,
            CodeSnippet         = post.CodeSnippet,
            ProgrammingLanguage = post.ProgrammingLanguage,
            Tags                = post.Tags,
            AuthorId            = post.AuthorId,
            AuthorName        = $"{post.Author.FirstName} {post.Author.LastName}",
            AuthorIsDeleted   = post.Author?.Status == UserStatus.Deleted,
            Category          = post.Category,
            IsPinned          = post.IsPinned,
            IsLocked          = post.IsLocked,
            ViewCount         = post.ViewCount,
            UpvoteCount       = upvotes,
            CommentCount      = post.Comments.Count,
            UserVote          = userVote is null ? 0 : userVote.IsUpvote ? 1 : -1,
            LearningPathTitle = post.LearningPath?.Title,
            GroupId           = post.GroupId,
            GroupName         = post.Group?.Name,
            Edited            = post.Edited,
            EditedAt          = post.EditedAt,
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
            AuthorIsDeleted = comment.Author?.Status == UserStatus.Deleted,
            PostId          = comment.PostId,
            ParentCommentId = comment.ParentCommentId,
            UpvoteCount     = upvotes,
            UserVote        = userVote is null ? 0 : userVote.IsUpvote ? 1 : -1,
            Replies         = comment.Replies?
                .Select(r => MapComment(r, userId))
                .ToList() ?? [],
            Edited          = comment.Edited,
            EditedAt        = comment.EditedAt,
            CreatedAt       = comment.CreatedAt,
            UpdatedAt       = comment.UpdatedAt,
        };
    }

    // ── Groups ─────────────────────────────────────────────────

    public async Task<GroupListResponseDto> GetGroupsAsync(
        string? search, bool? isPublic, int page, int pageSize, string userId)
    {
        var paged = await _repository.SearchGroupsAsync(
            search, isPublic, null, page, pageSize);

        var groups = new List<GroupDto>();
        foreach (var group in paged.Items)
        {
            var members = await _repository.GetGroupMembersAsync(group.Id);
            groups.Add(new GroupDto
            {
                Id          = group.Id,
                Name        = group.Name,
                Description = group.Description,
                OwnerId     = group.OwnerId,
                OwnerName   = $"{group.Owner.FirstName} {group.Owner.LastName}",
                IsPublic    = group.IsPublic,
                MemberCount = members.Count(),
                PostCount   = group.Posts.Count,
                CreatedAt   = group.CreatedAt,
            });
        }

        return new GroupListResponseDto
        {
            Groups     = groups,
            TotalCount = paged.TotalCount,
            Page       = paged.Page,
            PageSize   = paged.PageSize,
            TotalPages = paged.TotalPages,
        };
    }

    public async Task<GroupDetailDto> GetGroupByIdAsync(int groupId, string userId)
    {
        var group = await _repository.GetGroupWithMembersAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        var dto = MapGroupDetail(group);
        dto.CurrentUserRole = group.Members
            .FirstOrDefault(m => m.UserId == userId)?.Role;

        return dto;
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupDto dto, string userId)
    {
        await EnsureAdminAsync(userId);

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Group name is required.");

        if (await _context.Groups.AnyAsync(g => g.Name == dto.Name))
            throw new ArgumentException("A group with this name already exists.");

        var group = new Group
        {
            Name        = dto.Name,
            Description = dto.Description,
            IsPublic    = dto.IsPublic,
            OwnerId     = userId,
        };

        await _repository.AddGroupAsync(group);
        await _repository.JoinGroupAsync(new GroupMember
        {
            GroupId = group.Id,
            UserId  = userId,
            Role    = "Owner",
        });

        await _auditLog.LogAsync(
            AuditAction.GROUP_CREATED,
            "Group",
            group.Id.ToString(),
            $"Group '{group.Name}' was created.",
            additionalData: $"IsPublic: {group.IsPublic}",
            userId: userId);

        return await MapGroupAsync(group);
    }

    public async Task<GroupDto> UpdateGroupAsync(
        int groupId, UpdateGroupDto dto, string userId)
    {
        var group = await _context.Groups
            .Include(g => g.Posts)
            .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        await EnsureGroupModeratorAsync(group, userId);

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Group name is required.");

        if (await _context.Groups.AnyAsync(g => g.Name == dto.Name && g.Id != groupId))
            throw new ArgumentException("A group with this name already exists.");

        var oldValue      = $"Name: {group.Name}; IsPublic: {group.IsPublic}";

        group.Name        = dto.Name;
        group.Description = dto.Description;
        group.IsPublic    = dto.IsPublic;
        group.UpdatedAt   = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.GROUP_UPDATED,
            "Group",
            group.Id.ToString(),
            $"Group '{group.Name}' was updated.",
            oldValue: oldValue,
            newValue: $"Name: {group.Name}; IsPublic: {group.IsPublic}",
            userId: userId);

        return await MapGroupAsync(group);
    }

    public async Task DeleteGroupAsync(int groupId, string userId)
    {
        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        await EnsureGroupModeratorAsync(group, userId);

        var groupName = group.Name;
        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.GROUP_DELETED,
            "Group",
            groupId.ToString(),
            $"Group '{groupName}' was deleted.",
            userId: userId);
    }

    public async Task JoinGroupAsync(int groupId, string userId)
    {
        var group = await _repository.GetGroupByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        var existing = await _repository.GetGroupMemberAsync(groupId, userId);
        if (existing is not null)
            throw new ArgumentException("You are already a member of this group.");

        if (!group.IsPublic)
            throw new ArgumentException("This group is private.");

        await _repository.JoinGroupAsync(new GroupMember
        {
            GroupId = groupId,
            UserId  = userId,
            Role    = "Member",
        });

        await _auditLog.LogAsync(
            AuditAction.GROUP_JOINED,
            "Group",
            groupId.ToString(),
            $"Joined group '{group.Name}'.",
            userId: userId);
    }

    public async Task LeaveGroupAsync(int groupId, string userId)
    {
        var group = await _repository.GetGroupByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        if (group.OwnerId == userId)
            throw new ArgumentException("The group owner cannot leave their own group.");

        var left = await _repository.LeaveGroupAsync(groupId, userId);
        if (!left)
            throw new KeyNotFoundException("You are not a member of this group.");

        await _auditLog.LogAsync(
            AuditAction.GROUP_LEFT,
            "Group",
            groupId.ToString(),
            $"Left group '{group.Name}'.",
            userId: userId);
    }

    // ── Group posts ────────────────────────────────────────────

    public async Task<PostListResponseDto> GetGroupPostsAsync(
        int groupId, string? search, string? category,
        PostSortOrder sort, int page, int pageSize, string userId)
    {
        _ = await _repository.GetGroupByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        var paged = await _repository.GetPostsByGroupAsync(
            groupId, search, category, sort, page, pageSize);

        return MapPostList(paged, userId);
    }

    public async Task<PostSummaryDto> CreateGroupPostAsync(
        int groupId, CreatePostDto dto, string userId)
    {
        await EnsureActiveMembershipAsync(groupId, userId);

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required.");

        var post = new Post
        {
            Title               = dto.Title,
            Content             = dto.Content,
            CodeSnippet         = dto.CodeSnippet,
            ProgrammingLanguage = dto.ProgrammingLanguage,
            Tags                = dto.Tags,
            Category            = dto.Category,
            LearningPathId      = dto.LearningPathId,
            AuthorId            = userId,
            GroupId             = groupId,
        };

        await _repository.AddPostAsync(post);

        await LogPostAsync(AuditAction.POST_CREATED, post, userId,
            $"Post '{post.Title}' was created in a group.");

        await _context.Entry(post).Reference(p => p.Author).LoadAsync();

        return MapToSummary(post, userId);
    }

    public async Task<PostListResponseDto> GetTrendingPostsAsync(
        string? search, string? category, int? groupId,
        int page, int pageSize, string userId)
    {
        var candidatePageSize = Math.Max(pageSize * 10, 100);

        var paged = await _repository.SearchPostsAsync(
            search, category, groupId,
            PostSortOrder.Score, 1, candidatePageSize);

        var ranked = paged.Items
            .Select(p => (Post: p, Score: CalculateTrendingScore(p.Score, p.CreatedAt)))
            .OrderByDescending(x => x.Score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Post);

        return new PostListResponseDto
        {
            Posts      = ranked.Select(p => MapToSummary(p, userId)).ToList(),
            TotalCount = paged.TotalCount,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = (int)Math.Ceiling((double)paged.TotalCount / pageSize),
        };
    }

    // ── Pinning ────────────────────────────────────────────────

    public async Task<PostSummaryDto> PinPostAsync(int postId, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.LearningPath)
            .Include(p => p.Group)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        if (!post.GroupId.HasValue)
            throw new ArgumentException("Only posts inside a group can be pinned.");

        var group = await _repository.GetGroupByIdAsync(post.GroupId.Value);
        await EnsureGroupModeratorAsync(group, userId);

        var pinned = await _context.Posts
            .Where(p => p.IsPinned && p.GroupId == post.GroupId.Value)
            .ToListAsync();

        foreach (var other in pinned)
        {
            if (other.Id == postId) continue;

            other.IsPinned  = false;
            other.UpdatedAt = DateTime.UtcNow;
        }

        post.IsPinned  = true;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogPostAsync(AuditAction.POST_PINNED, post, userId,
            $"Post '{post.Title}' was pinned.");

        return MapToSummary(post, userId);
    }

    public async Task<PostSummaryDto> UnpinPostAsync(int postId, string userId)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.LearningPath)
            .Include(p => p.Group)
            .Include(p => p.Votes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId)
            ?? throw new KeyNotFoundException("Post not found.");

        if (post.GroupId.HasValue)
        {
            var group = await _repository.GetGroupByIdAsync(post.GroupId.Value);
            await EnsureGroupModeratorAsync(group, userId);
        }
        else
        {
            await EnsureAdminAsync(userId);
        }

        post.IsPinned  = false;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogPostAsync(AuditAction.POST_UNPINNED, post, userId,
            $"Post '{post.Title}' was unpinned.");

        return MapToSummary(post, userId);
    }

    // ── Membership moderation ──────────────────────────────────

    public async Task BanUserFromGroupAsync(
        int groupId, string targetUserId, string adminUserId)
    {
        var group = await _repository.GetGroupByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        await EnsureGroupModeratorAsync(group, adminUserId);

        if (group.OwnerId == targetUserId)
            throw new ArgumentException("You cannot ban the group owner.");

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId)
            ?? throw new KeyNotFoundException("User is not a member of this group.");

        member.Role = "Banned";
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.GROUP_MEMBER_BANNED,
            "Group",
            groupId.ToString(),
            $"A member was banned from group '{group.Name}'.",
            additionalData: $"BannedUserId: {targetUserId}",
            userId: adminUserId);
    }

    public async Task UnbanUserFromGroupAsync(
        int groupId, string targetUserId, string adminUserId)
    {
        var group = await _repository.GetGroupByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        await EnsureGroupModeratorAsync(group, adminUserId);

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == targetUserId)
            ?? throw new KeyNotFoundException("User is not a member of this group.");

        member.Role = "Member";
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.GROUP_MEMBER_UNBANNED,
            "Group",
            groupId.ToString(),
            $"A member was unbanned in group '{group.Name}'.",
            additionalData: $"UnbannedUserId: {targetUserId}",
            userId: adminUserId);
    }

    // ── Reports / moderation ───────────────────────────────────

    public async Task ReportPostAsync(int postId, CreateReportDto dto, string userId)
    {
        _ = await _repository.GetPostByIdAsync(postId)
            ?? throw new KeyNotFoundException("Post not found.");

        await AddReportAsync("Post", postId, dto, userId);
    }

    public async Task ReportCommentAsync(int commentId, CreateReportDto dto, string userId)
    {
        _ = await _repository.GetCommentByIdAsync(commentId)
            ?? throw new KeyNotFoundException("Comment not found.");

        await AddReportAsync("Comment", commentId, dto, userId);
    }

    public async Task<ReportListResponseDto> GetModerationQueueAsync(
        int page, int pageSize, string userId)
    {
        await EnsureAdminAsync(userId);

        var query = _context.Reports
            .AsNoTracking()
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var reports = new List<ReportDto>();
        foreach (var report in items)
        {
            var reportedByName = await _context.Users
                .Where(u => u.Id == report.ReportedByUserId)
                .Select(u => u.FirstName + " " + u.LastName)
                .FirstOrDefaultAsync() ?? "Unknown";

            reports.Add(new ReportDto
            {
                Id               = report.Id,
                TargetType       = report.TargetType,
                TargetId         = report.TargetId,
                Reason           = report.Reason,
                Status           = report.Status,
                ReportedByUserId = report.ReportedByUserId,
                ReportedByName   = reportedByName,
                CreatedAt        = report.CreatedAt,
            });
        }

        return new ReportListResponseDto
        {
            Reports    = reports,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
        };
    }

    public async Task ResolveReportAsync(int reportId, string userId)
    {
        await EnsureAdminAsync(userId);

        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException("Report not found.");

        report.Status = "Resolved";
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.REPORT_RESOLVED,
            "Report",
            report.Id.ToString(),
            $"Report on {report.TargetType} #{report.TargetId} was resolved.",
            userId: userId);
    }

    public async Task DismissReportAsync(int reportId, string userId)
    {
        await EnsureAdminAsync(userId);

        var report = await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new KeyNotFoundException("Report not found.");

        report.Status = "Dismissed";
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.REPORT_DISMISSED,
            "Report",
            report.Id.ToString(),
            $"Report on {report.TargetType} #{report.TargetId} was dismissed.",
            userId: userId);
    }

    // ── Business rules / helpers ───────────────────────────────

    public static double CalculateTrendingScore(int score, DateTime createdAt)
    {
        var ageHours = Math.Max((DateTime.UtcNow - createdAt).TotalHours, 0);
        return score / Math.Pow(ageHours + 2, 1.5);
    }

    private async Task AddReportAsync(
        string targetType, int targetId, CreateReportDto dto, string userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("A reason is required.");

        var report = new Report
        {
            TargetType      = targetType,
            TargetId        = targetId,
            Reason          = dto.Reason,
            Status          = "Pending",
            ReportedByUserId = userId,
        };

        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.REPORT_CREATED,
            "Report",
            report.Id.ToString(),
            $"{targetType} #{targetId} was reported.",
            additionalData: Truncate(dto.Reason),
            userId: userId);
    }

    private Task LogPostAsync(
        AuditAction action, Post post, string userId, string description,
        string? oldValue = null, string? newValue = null) =>
        _auditLog.LogAsync(
            action,
            "Post",
            post.Id.ToString(),
            description,
            oldValue: oldValue,
            newValue: newValue,
            additionalData: post.GroupId.HasValue ? $"GroupId: {post.GroupId}" : null,
            userId: userId);

    private static string Truncate(string value) =>
        value.Length > 500 ? value[..500] : value;

    private async Task<bool> IsAdminAsync(string userId)
    {
        var adminRoleId = await _context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        return adminRoleId != null && await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRoleId);
    }

    private async Task EnsureAdminAsync(string userId)
    {
        if (!await IsAdminAsync(userId))
            throw new UnauthorizedAccessException("Admin privileges required.");
    }

    private async Task EnsureGroupModeratorAsync(Group? group, string userId)
    {
        if (group is null)
            throw new KeyNotFoundException("Group not found.");

        if (group.OwnerId == userId)
            return;

        if (await IsAdminAsync(userId))
            return;

        throw new UnauthorizedAccessException("Group owner or admin privileges required.");
    }

    private async Task EnsureActiveMembershipAsync(int groupId, string userId)
    {
        var member = await _repository.GetGroupMemberAsync(groupId, userId);

        if (member is null)
            throw new UnauthorizedAccessException(
                "You must join this group before posting or commenting.");

        if (member.Role == "Banned")
            throw new UnauthorizedAccessException("You are banned from this group.");
    }

    private async Task<GroupDto> MapGroupAsync(Group group)
    {
        var ownerName = await _context.Users
            .Where(u => u.Id == group.OwnerId)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync() ?? "Unknown";

        var members = await _repository.GetGroupMembersAsync(group.Id);

        return new GroupDto
        {
            Id          = group.Id,
            Name        = group.Name,
            Description = group.Description,
            OwnerId     = group.OwnerId,
            OwnerName   = ownerName,
            IsPublic    = group.IsPublic,
            MemberCount = members.Count(),
            PostCount   = group.Posts.Count,
            CreatedAt   = group.CreatedAt,
        };
    }

    private static GroupDetailDto MapGroupDetail(Group group)
    {
        return new GroupDetailDto
        {
            Id          = group.Id,
            Name        = group.Name,
            Description = group.Description,
            OwnerId     = group.OwnerId,
            OwnerName   = $"{group.Owner.FirstName} {group.Owner.LastName}",
            IsPublic    = group.IsPublic,
            MemberCount = group.Members.Count,
            PostCount   = group.Posts.Count,
            CreatedAt   = group.CreatedAt,
            Members     = group.Members
                .Select(m => new GroupMemberDto
                {
                    UserId   = m.UserId,
                    UserName = $"{m.User.FirstName} {m.User.LastName}",
                    Role     = m.Role,
                    JoinedAt = m.JoinedAt,
                })
                .OrderBy(m => m.JoinedAt)
                .ToList(),
        };
    }

    private static PostListResponseDto MapPostList(
        PagedResult<Post> paged, string userId)
    {
        return new PostListResponseDto
        {
            Posts      = paged.Items.Select(p => MapToSummary(p, userId)).ToList(),
            TotalCount = paged.TotalCount,
            Page       = paged.Page,
            PageSize   = paged.PageSize,
            TotalPages = paged.TotalPages,
        };
    }
}
