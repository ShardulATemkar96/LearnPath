using LearnPath.API.Common;
using LearnPath.API.DTOs.Community;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnPath.API.Controllers;

[ApiController]
[Route("api/v1/community")]
[Authorize]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _service;
    public CommunityController(ICommunityService service) => _service = service;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetPostsAsync(
            category, search, page, pageSize, userId);
        return Ok(ApiResponse<PostListResponseDto>.Ok(result));
    }

    [HttpGet("posts/{postId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(int postId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _service.GetPostByIdAsync(postId, userId);
        return Ok(ApiResponse<PostDetailDto>.Ok(result));
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var result = await _service.CreatePostAsync(dto, UserId);
        return CreatedAtAction(nameof(GetPost), new { postId = result.Id },
            ApiResponse<PostSummaryDto>.Ok(result, "Post created."));
    }

    [HttpPut("posts/{postId:int}")]
    public async Task<IActionResult> UpdatePost(
        int postId, [FromBody] UpdatePostDto dto)
    {
        var result = await _service.UpdatePostAsync(postId, dto, UserId);
        return Ok(ApiResponse<PostSummaryDto>.Ok(result, "Post updated."));
    }

    [HttpDelete("posts/{postId:int}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        await _service.DeletePostAsync(postId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Post deleted."));
    }

    [HttpPost("posts/{postId:int}/vote")]
    public async Task<IActionResult> VotePost(
        int postId, [FromBody] VoteDto dto)
    {
        var count = await _service.VotePostAsync(postId, dto, UserId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPost("posts/{postId:int}/comments")]
    public async Task<IActionResult> AddComment(
        int postId, [FromBody] CreateCommentDto dto)
    {
        var result = await _service.AddCommentAsync(postId, dto, UserId);
        return Ok(ApiResponse<CommentDto>.Ok(result, "Comment added."));
    }

    [HttpPut("comments/{commentId:int}")]
    public async Task<IActionResult> UpdateComment(
        int commentId, [FromBody] UpdateCommentDto dto)
    {
        var result = await _service.UpdateCommentAsync(commentId, dto, UserId);
        return Ok(ApiResponse<CommentDto>.Ok(result, "Comment updated."));
    }

    [HttpDelete("comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        await _service.DeleteCommentAsync(commentId, UserId);
        return Ok(ApiResponse<object>.Ok(null!, "Comment deleted."));
    }

    [HttpPost("comments/{commentId:int}/vote")]
    public async Task<IActionResult> VoteComment(
        int commentId, [FromBody] VoteDto dto)
    {
        var count = await _service.VoteCommentAsync(commentId, dto, UserId);
        return Ok(ApiResponse<int>.Ok(count));
    }
}
