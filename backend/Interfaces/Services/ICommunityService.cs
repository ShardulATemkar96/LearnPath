using LearnPath.API.DTOs.Community;

namespace LearnPath.API.Interfaces.Services;

public interface ICommunityService
{
    Task<PostListResponseDto> GetPostsAsync(
        string? category, string? search, int page, int pageSize, string userId);
    Task<PostDetailDto> GetPostByIdAsync(int postId, string userId);
    Task<PostSummaryDto> CreatePostAsync(CreatePostDto dto, string userId);
    Task<PostSummaryDto> UpdatePostAsync(int postId, UpdatePostDto dto, string userId);
    Task DeletePostAsync(int postId, string userId);
    Task<int> VotePostAsync(int postId, VoteDto dto, string userId);

    Task<CommentDto> AddCommentAsync(int postId, CreateCommentDto dto, string userId);
    Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentDto dto, string userId);
    Task DeleteCommentAsync(int commentId, string userId);
    Task<int> VoteCommentAsync(int commentId, VoteDto dto, string userId);
}
