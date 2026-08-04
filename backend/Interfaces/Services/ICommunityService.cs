using LearnPath.API.DTOs.Community;
using LearnPath.API.Repositories;

namespace LearnPath.API.Interfaces.Services;

public interface ICommunityService
{
    Task<PostListResponseDto> GetPostsAsync(
        string? category, string? search, string? tag, PostSortOrder sort,
        PostFilter filter, int page, int pageSize, string userId);
    Task<PostDetailDto> GetPostByIdAsync(int postId, string userId);
    Task<PostSummaryDto> CreatePostAsync(CreatePostDto dto, string userId);
    Task<PostSummaryDto> UpdatePostAsync(int postId, UpdatePostDto dto, string userId);
    Task DeletePostAsync(int postId, string userId);
    Task<int> VotePostAsync(int postId, VoteDto dto, string userId);

    Task<CommentDto> AddCommentAsync(int postId, CreateCommentDto dto, string userId);
    Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentDto dto, string userId);
    Task DeleteCommentAsync(int commentId, string userId);
    Task<int> VoteCommentAsync(int commentId, VoteDto dto, string userId);
    Task<List<CommentDto>> GetPostCommentsAsync(int postId, string userId);
    Task RemovePostVoteAsync(int postId, string userId);
    Task RemoveCommentVoteAsync(int commentId, string userId);

    // Groups
    Task<GroupListResponseDto> GetGroupsAsync(
        string? search, bool? isPublic, int page, int pageSize, string userId);
    Task<GroupDetailDto> GetGroupByIdAsync(int groupId, string userId);
    Task<GroupDto> CreateGroupAsync(CreateGroupDto dto, string userId);
    Task<GroupDto> UpdateGroupAsync(int groupId, UpdateGroupDto dto, string userId);
    Task DeleteGroupAsync(int groupId, string userId);
    Task JoinGroupAsync(int groupId, string userId);
    Task LeaveGroupAsync(int groupId, string userId);

    // Group posts
    Task<PostListResponseDto> GetGroupPostsAsync(
        int groupId, string? search, string? category,
        PostSortOrder sort, int page, int pageSize, string userId);
    Task<PostSummaryDto> CreateGroupPostAsync(int groupId, CreatePostDto dto, string userId);
    Task<PostListResponseDto> GetTrendingPostsAsync(
        string? search, string? category, int? groupId, int page, int pageSize, string userId);

    // Pinning
    Task<PostSummaryDto> PinPostAsync(int postId, string userId);
    Task<PostSummaryDto> UnpinPostAsync(int postId, string userId);

    // Membership moderation
    Task BanUserFromGroupAsync(int groupId, string targetUserId, string adminUserId);
    Task UnbanUserFromGroupAsync(int groupId, string targetUserId, string adminUserId);

    // Reports / moderation
    Task ReportPostAsync(int postId, CreateReportDto dto, string userId);
    Task ReportCommentAsync(int commentId, CreateReportDto dto, string userId);
    Task<ReportListResponseDto> GetModerationQueueAsync(int page, int pageSize, string userId);
    Task ResolveReportAsync(int reportId, string userId);
    Task DismissReportAsync(int reportId, string userId);
}
