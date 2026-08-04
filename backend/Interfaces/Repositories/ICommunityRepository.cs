using LearnPath.API.Entities;
using LearnPath.API.Repositories;

namespace LearnPath.API.Interfaces.Repositories;

public interface ICommunityRepository
{
    // Groups
    Task<Group?> GetGroupByIdAsync(int groupId);
    Task<Group?> GetGroupWithMembersAsync(int groupId);
    Task<PagedResult<Group>> SearchGroupsAsync(
        string? search, bool? isPublic, string? ownerId, int page, int pageSize);
    Task<IEnumerable<Group>> GetJoinedGroupsAsync(string userId);
    Task<IEnumerable<Group>> GetOwnedGroupsAsync(string userId);
    Task<IEnumerable<GroupMember>> GetGroupMembersAsync(int groupId);
    Task<GroupMember?> GetGroupMemberAsync(int groupId, string userId);
    Task<bool> IsGroupMemberAsync(int groupId, string userId);
    Task AddGroupAsync(Group group);
    Task UpdateGroup(Group group);
    Task RemoveGroup(Group group);
    Task JoinGroupAsync(GroupMember member);
    Task<bool> LeaveGroupAsync(int groupId, string userId);

    // Posts
    Task<Post?> GetPostByIdAsync(int postId);
    Task<Post?> GetPostWithDetailsAsync(int postId);
    Task<PagedResult<Post>> SearchPostsAsync(
        string? search, string? category, int? groupId,
        PostSortOrder sort, int page, int pageSize);
    Task<PagedResult<Post>> GetPostsByGroupAsync(
        int groupId, string? search, string? category,
        PostSortOrder sort, int page, int pageSize);
    Task<IEnumerable<Post>> GetPinnedPostsAsync(int? groupId = null);
    Task AddPostAsync(Post post);
    Task UpdatePost(Post post);
    Task RemovePost(Post post);

    // Comments
    Task<Comment?> GetCommentByIdAsync(int commentId);
    Task<IEnumerable<Comment>> GetPostCommentsAsync(int postId);
    Task<IEnumerable<Comment>> GetNestedRepliesAsync(int commentId);
    Task AddCommentAsync(Comment comment);
    Task UpdateComment(Comment comment);
    Task RemoveComment(Comment comment);

    // Votes
    Task<PostVote?> GetUserPostVoteAsync(string userId, int postId);
    Task<CommentVote?> GetUserCommentVoteAsync(string userId, int commentId);
    Task AddPostVoteAsync(PostVote vote);
    Task UpdatePostVote(PostVote vote);
    Task RemovePostVote(PostVote vote);
    Task AddCommentVoteAsync(CommentVote vote);
    Task UpdateCommentVote(CommentVote vote);
    Task RemoveCommentVote(CommentVote vote);

    // Persistence
    Task<int> SaveChangesAsync();
}
