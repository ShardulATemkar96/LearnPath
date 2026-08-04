using FluentValidation;
using LearnPath.API.Common;
using LearnPath.API.DTOs.Community;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/community")]
[Authorize]
[Produces("application/json")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _service;
    private readonly IValidator<CreateGroupDto>   _createGroupValidator;
    private readonly IValidator<UpdateGroupDto>   _updateGroupValidator;
    private readonly IValidator<CreatePostDto>    _createPostValidator;
    private readonly IValidator<UpdatePostDto>    _updatePostValidator;
    private readonly IValidator<CreateCommentDto> _createCommentValidator;
    private readonly IValidator<UpdateCommentDto> _updateCommentValidator;
    private readonly IValidator<VoteDto>          _voteValidator;
    private readonly IValidator<CreateReportDto>  _reportValidator;

    public CommunityController(
        ICommunityService service,
        IValidator<CreateGroupDto>   createGroupValidator,
        IValidator<UpdateGroupDto>   updateGroupValidator,
        IValidator<CreatePostDto>    createPostValidator,
        IValidator<UpdatePostDto>    updatePostValidator,
        IValidator<CreateCommentDto> createCommentValidator,
        IValidator<UpdateCommentDto> updateCommentValidator,
        IValidator<VoteDto>          voteValidator,
        IValidator<CreateReportDto>  reportValidator)
    {
        _service = service;
        _createGroupValidator   = createGroupValidator;
        _updateGroupValidator   = updateGroupValidator;
        _createPostValidator    = createPostValidator;
        _updatePostValidator    = updatePostValidator;
        _createCommentValidator = createCommentValidator;
        _updateCommentValidator = updateCommentValidator;
        _voteValidator          = voteValidator;
        _reportValidator        = reportValidator;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Posts ─────────────────────────────────────────────────

    /// <summary>Get a paged list of community posts, optionally filtered by category and search.</summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="search">Optional free-text search.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <returns>A paged list of post summaries.</returns>
    /// <response code="200">Posts returned.</response>
    /// <response code="400">Invalid pagination.</response>
    [HttpGet("posts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PostListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] string? tag,
        [FromQuery] PostSortOrder sort = PostSortOrder.Newest,
        [FromQuery] PostFilter filter = PostFilter.All,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var invalid = ValidatePagination(page, pageSize);
        if (invalid is not null) return invalid;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetPostsAsync(
            category, search, tag, sort, filter, page, pageSize, userId);
        return Ok(ApiResponse<PostListResponseDto>.Ok(result));
    }

    /// <summary>Get a single post with its full content and comment tree.</summary>
    /// <param name="postId">Post id.</param>
    /// <returns>Post details including top-level comments.</returns>
    /// <response code="200">Post returned.</response>
    /// <response code="404">Post not found.</response>
    [HttpGet("posts/{postId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PostDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPost(int postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetPostByIdAsync(postId, userId);
        return Ok(ApiResponse<PostDetailDto>.Ok(result));
    }

    /// <summary>Create a new community post.</summary>
    /// <param name="dto">Post payload.</param>
    /// <returns>The created post summary.</returns>
    /// <response code="201">Post created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    [HttpPost("posts")]
    [ProducesResponseType(typeof(ApiResponse<PostSummaryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var invalid = Validate(_createPostValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.CreatePostAsync(dto, UserId);
        return CreatedAtAction(nameof(GetPost), new { postId = result.Id },
            ApiResponse<PostSummaryDto>.Ok(result, "Post created."));
    }

    /// <summary>Update an existing post owned by the current user.</summary>
    /// <param name="postId">Post id.</param>
    /// <param name="dto">Updated post payload.</param>
    /// <returns>The updated post summary.</returns>
    /// <response code="200">Post updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpPut("posts/{postId:int}")]
    [ProducesResponseType(typeof(ApiResponse<PostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePost(
        int postId, [FromBody] UpdatePostDto dto)
    {
        var invalid = Validate(_updatePostValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.UpdatePostAsync(postId, dto, UserId);
        return Ok(ApiResponse<PostSummaryDto>.Ok(result, "Post updated."));
    }

    /// <summary>Delete a post owned by the current user (or by an admin).</summary>
    /// <param name="postId">Post id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Post deleted.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpDelete("posts/{postId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePost(int postId)
    {
        await _service.DeletePostAsync(postId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Post deleted."));
    }

    /// <summary>Vote on a post (upvote/downvote, or toggle to remove an identical vote).</summary>
    /// <param name="postId">Post id.</param>
    /// <param name="dto">Vote direction.</param>
    /// <returns>The current upvote count.</returns>
    /// <response code="200">Vote applied.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpPost("posts/{postId:int}/vote")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VotePost(
        int postId, [FromBody] VoteDto dto)
    {
        var invalid = Validate(_voteValidator, dto);
        if (invalid is not null) return invalid;

        var count = await _service.VotePostAsync(postId, dto, UserId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>Remove the current user's vote from a post.</summary>
    /// <param name="postId">Post id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Vote removed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpDelete("posts/{postId:int}/vote")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePostVote(int postId)
    {
        await _service.RemovePostVoteAsync(postId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Vote removed."));
    }

    // ── Comments ───────────────────────────────────────────────

    /// <summary>Get the top-level comments for a post (with nested replies).</summary>
    /// <param name="postId">Post id.</param>
    /// <returns>List of comment threads.</returns>
    /// <response code="200">Comments returned.</response>
    /// <response code="404">Post not found.</response>
    [HttpGet("posts/{postId:int}/comments")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostComments(int postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetPostCommentsAsync(postId, userId);
        return Ok(ApiResponse<List<CommentDto>>.Ok(result));
    }

    /// <summary>Add a comment (or reply) to a post.</summary>
    /// <param name="postId">Post id.</param>
    /// <param name="dto">Comment payload.</param>
    /// <returns>The created comment.</returns>
    /// <response code="200">Comment added.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpPost("posts/{postId:int}/comments")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(
        int postId, [FromBody] CreateCommentDto dto)
    {
        var invalid = Validate(_createCommentValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.AddCommentAsync(postId, dto, UserId);
        return Ok(ApiResponse<CommentDto>.Ok(result, "Comment added."));
    }

    /// <summary>Update a comment owned by the current user.</summary>
    /// <param name="commentId">Comment id.</param>
    /// <param name="dto">Updated comment payload.</param>
    /// <returns>The updated comment.</returns>
    /// <response code="200">Comment updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Comment not found.</response>
    [HttpPut("comments/{commentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComment(
        int commentId, [FromBody] UpdateCommentDto dto)
    {
        var invalid = Validate(_updateCommentValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.UpdateCommentAsync(commentId, dto, UserId);
        return Ok(ApiResponse<CommentDto>.Ok(result, "Comment updated."));
    }

    /// <summary>Delete a comment owned by the current user (or by an admin).</summary>
    /// <param name="commentId">Comment id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Comment deleted.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Comment not found.</response>
    [HttpDelete("comments/{commentId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        await _service.DeleteCommentAsync(commentId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Comment deleted."));
    }

    /// <summary>Vote on a comment (upvote/downvote, or toggle to remove an identical vote).</summary>
    /// <param name="commentId">Comment id.</param>
    /// <param name="dto">Vote direction.</param>
    /// <returns>The current upvote count.</returns>
    /// <response code="200">Vote applied.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Comment not found.</response>
    [HttpPost("comments/{commentId:int}/vote")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoteComment(
        int commentId, [FromBody] VoteDto dto)
    {
        var invalid = Validate(_voteValidator, dto);
        if (invalid is not null) return invalid;

        var count = await _service.VoteCommentAsync(commentId, dto, UserId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>Remove the current user's vote from a comment.</summary>
    /// <param name="commentId">Comment id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Vote removed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Comment not found.</response>
    [HttpDelete("comments/{commentId:int}/vote")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCommentVote(int commentId)
    {
        await _service.RemoveCommentVoteAsync(commentId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Vote removed."));
    }

    // ── Groups ─────────────────────────────────────────────────

    /// <summary>Get a paged list of community groups.</summary>
    /// <param name="search">Optional free-text search.</param>
    /// <param name="isPublic">Optional public/private filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <returns>A paged list of groups.</returns>
    /// <response code="200">Groups returned.</response>
    /// <response code="400">Invalid pagination.</response>
    [HttpGet("groups")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GroupListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroups(
        [FromQuery] string? search,
        [FromQuery] bool? isPublic,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var invalid = ValidatePagination(page, pageSize);
        if (invalid is not null) return invalid;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetGroupsAsync(
            search, isPublic, page, pageSize, userId);
        return Ok(ApiResponse<GroupListResponseDto>.Ok(result));
    }

    /// <summary>Search community groups by name or description.</summary>
    /// <param name="search">Free-text search.</param>
    /// <param name="isPublic">Optional public/private filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <returns>A paged list of matching groups.</returns>
    /// <response code="200">Groups returned.</response>
    /// <response code="400">Invalid pagination.</response>
    [HttpGet("groups/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GroupListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchGroups(
        [FromQuery] string? search,
        [FromQuery] bool? isPublic,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return await GetGroups(search, isPublic, page, pageSize);
    }

    /// <summary>Get a group by id, including its members.</summary>
    /// <param name="groupId">Group id.</param>
    /// <returns>Group details.</returns>
    /// <response code="200">Group returned.</response>
    /// <response code="404">Group not found.</response>
    [HttpGet("groups/{groupId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GroupDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(int groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetGroupByIdAsync(groupId, userId);
        return Ok(ApiResponse<GroupDetailDto>.Ok(result));
    }

    /// <summary>Create a new group (admin only, enforced by the service).</summary>
    /// <param name="dto">Group payload.</param>
    /// <returns>The created group.</returns>
    /// <response code="201">Group created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    [HttpPost("groups")]
    [ProducesResponseType(typeof(ApiResponse<GroupDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        var invalid = Validate(_createGroupValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.CreateGroupAsync(dto, UserId);
        return CreatedAtAction(nameof(GetGroup), new { groupId = result.Id },
            ApiResponse<GroupDto>.Ok(result, "Group created."));
    }

    /// <summary>Update a group (group owner or admin, enforced by the service).</summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="dto">Updated group payload.</param>
    /// <returns>The updated group.</returns>
    /// <response code="200">Group updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Group not found.</response>
    [HttpPut("groups/{groupId:int}")]
    [ProducesResponseType(typeof(ApiResponse<GroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGroup(
        int groupId, [FromBody] UpdateGroupDto dto)
    {
        var invalid = Validate(_updateGroupValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.UpdateGroupAsync(groupId, dto, UserId);
        return Ok(ApiResponse<GroupDto>.Ok(result, "Group updated."));
    }

    /// <summary>Delete a group (group owner or admin, enforced by the service).</summary>
    /// <param name="groupId">Group id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Group deleted.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Group not found.</response>
    [HttpDelete("groups/{groupId:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(int groupId)
    {
        await _service.DeleteGroupAsync(groupId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Group deleted."));
    }

    /// <summary>Join a public group.</summary>
    /// <param name="groupId">Group id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Joined.</response>
    /// <response code="400">Already a member or group is private.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Group not found.</response>
    [HttpPost("groups/{groupId:int}/join")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinGroup(int groupId)
    {
        await _service.JoinGroupAsync(groupId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Joined group."));
    }

    /// <summary>Leave a group.</summary>
    /// <param name="groupId">Group id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Left.</response>
    /// <response code="400">Group owner cannot leave.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Group not found.</response>
    [HttpPost("groups/{groupId:int}/leave")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveGroup(int groupId)
    {
        await _service.LeaveGroupAsync(groupId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Left group."));
    }

    /// <summary>Ban a member from a group (group owner or admin, enforced by the service).</summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="userId">Target user id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Member banned.</response>
    /// <response code="400">Cannot ban the group owner.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Not a group moderator.</response>
    /// <response code="404">Group or member not found.</response>
    [HttpPost("groups/{groupId:int}/members/{userId}/ban")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUserFromGroup(int groupId, string userId)
    {
        await _service.BanUserFromGroupAsync(groupId, userId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Member banned."));
    }

    /// <summary>Unban a member from a group (group owner or admin, enforced by the service).</summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="userId">Target user id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Member unbanned.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Not a group moderator.</response>
    /// <response code="404">Group or member not found.</response>
    [HttpPost("groups/{groupId:int}/members/{userId}/unban")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbanUserFromGroup(int groupId, string userId)
    {
        await _service.UnbanUserFromGroupAsync(groupId, userId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Member unbanned."));
    }

    /// <summary>Get a paged list of posts within a group.</summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="search">Optional free-text search.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="sort">Sort order: Newest, Score, Pinned.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <returns>A paged list of post summaries.</returns>
    /// <response code="200">Posts returned.</response>
    /// <response code="400">Invalid pagination.</response>
    /// <response code="404">Group not found.</response>
    [HttpGet("groups/{groupId:int}/posts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PostListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupPosts(
        int groupId,
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] PostSortOrder sort = PostSortOrder.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var invalid = ValidatePagination(page, pageSize);
        if (invalid is not null) return invalid;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetGroupPostsAsync(
            groupId, search, category, sort, page, pageSize, userId);
        return Ok(ApiResponse<PostListResponseDto>.Ok(result));
    }

    /// <summary>Create a post inside a group (membership required, enforced by the service).</summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="dto">Post payload.</param>
    /// <returns>The created post summary.</returns>
    /// <response code="201">Post created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Group not found.</response>
    [HttpPost("groups/{groupId:int}/posts")]
    [ProducesResponseType(typeof(ApiResponse<PostSummaryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateGroupPost(
        int groupId, [FromBody] CreatePostDto dto)
    {
        var invalid = Validate(_createPostValidator, dto);
        if (invalid is not null) return invalid;

        var result = await _service.CreateGroupPostAsync(groupId, dto, UserId);
        return CreatedAtAction(nameof(GetPost), new { postId = result.Id },
            ApiResponse<PostSummaryDto>.Ok(result, "Post created."));
    }

    /// <summary>Search community posts ranked by trending score (score + recency).</summary>
    /// <param name="search">Optional free-text search.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="groupId">Optional group filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <returns>A paged list of trending post summaries.</returns>
    /// <response code="200">Posts returned.</response>
    /// <response code="400">Invalid pagination.</response>
    [HttpGet("posts/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PostListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPosts(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var invalid = ValidatePagination(page, pageSize);
        if (invalid is not null) return invalid;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetTrendingPostsAsync(
            search, category, groupId, page, pageSize, userId);
        return Ok(ApiResponse<PostListResponseDto>.Ok(result));
    }

    // ── Reports ────────────────────────────────────────────────

    /// <summary>Report a post for moderation.</summary>
    /// <param name="postId">Post id.</param>
    /// <param name="dto">Report reason.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Report submitted.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpPost("posts/{postId:int}/report")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportPost(
        int postId, [FromBody] CreateReportDto dto)
    {
        var invalid = Validate(_reportValidator, dto);
        if (invalid is not null) return invalid;

        await _service.ReportPostAsync(postId, dto, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Report submitted."));
    }

    /// <summary>Report a comment for moderation.</summary>
    /// <param name="commentId">Comment id.</param>
    /// <param name="dto">Report reason.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Report submitted.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Comment not found.</response>
    [HttpPost("comments/{commentId:int}/report")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportComment(
        int commentId, [FromBody] CreateReportDto dto)
    {
        var invalid = Validate(_reportValidator, dto);
        if (invalid is not null) return invalid;

        await _service.ReportCommentAsync(commentId, dto, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Report submitted."));
    }

    // ── Moderation queue ──────────────────────────────────────

    /// <summary>Get the pending moderation queue (admin only).</summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100).</param>
    /// <returns>A paged list of pending reports.</returns>
    /// <response code="200">Reports returned.</response>
    /// <response code="400">Invalid pagination.</response>
    /// <response code="401">Admin privileges required.</response>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(ApiResponse<ReportListResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetModerationQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var invalid = ValidatePagination(page, pageSize);
        if (invalid is not null) return invalid;

        var result = await _service.GetModerationQueueAsync(page, pageSize, UserId);
        return Ok(ApiResponse<ReportListResponseDto>.Ok(result));
    }

    /// <summary>Resolve a report (admin only).</summary>
    /// <param name="reportId">Report id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Report resolved.</response>
    /// <response code="401">Admin privileges required.</response>
    /// <response code="404">Report not found.</response>
    [HttpPost("reports/{reportId:int}/resolve")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveReport(int reportId)
    {
        await _service.ResolveReportAsync(reportId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Report resolved."));
    }

    /// <summary>Dismiss a report (admin only).</summary>
    /// <param name="reportId">Report id.</param>
    /// <returns>Confirmation message.</returns>
    /// <response code="200">Report dismissed.</response>
    /// <response code="401">Admin privileges required.</response>
    /// <response code="404">Report not found.</response>
    [HttpPost("reports/{reportId:int}/dismiss")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissReport(int reportId)
    {
        await _service.DismissReportAsync(reportId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Report dismissed."));
    }

    // ── Pinned Announcement ────────────────────────────────────

    /// <summary>Pin a group post as the group's single announcement (owner or admin, enforced by the service).</summary>
    /// <param name="postId">Post id.</param>
    /// <returns>The pinned post summary.</returns>
    /// <response code="200">Post pinned.</response>
    /// <response code="400">Post is not inside a group.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpPost("posts/{postId:int}/pin")]
    [ProducesResponseType(typeof(ApiResponse<PostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PinPost(int postId)
    {
        var result = await _service.PinPostAsync(postId, UserId);
        return Ok(ApiResponse<PostSummaryDto>.Ok(result, "Post pinned."));
    }

    /// <summary>Unpin a group post (owner or admin, enforced by the service).</summary>
    /// <param name="postId">Post id.</param>
    /// <returns>The unpinned post summary.</returns>
    /// <response code="200">Post unpinned.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Post not found.</response>
    [HttpDelete("posts/{postId:int}/pin")]
    [ProducesResponseType(typeof(ApiResponse<PostSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnpinPost(int postId)
    {
        var result = await _service.UnpinPostAsync(postId, UserId);
        return Ok(ApiResponse<PostSummaryDto>.Ok(result, "Post unpinned."));
    }

    // ── Helpers ────────────────────────────────────────────────

    private IActionResult? Validate<T>(IValidator<T> validator, T dto)
    {
        var result = validator.Validate(dto);
        if (result.IsValid) return null;

        return BadRequest(ApiResponse<object>.Fail(
            "Validation failed.",
            result.Errors.Select(e => e.ErrorMessage).ToList()));
    }

    private IActionResult? ValidatePagination(int page, int pageSize)
    {
        if (page < 1)
            return BadRequest(ApiResponse<object>.Fail("Page must be 1 or greater."));

        if (pageSize is < 1 or > 100)
            return BadRequest(ApiResponse<object>.Fail("Page size must be between 1 and 100."));

        return null;
    }
}
