import { createAsyncThunk, createSlice, PayloadAction } from "@reduxjs/toolkit";
import { communityService } from "../../services/communityService";
import {
  PostSummary, PostDetail, Comment,
  CreatePostRequest, CreateCommentRequest,
  Group, GroupDetail,
  CreateGroupRequest, UpdateGroupRequest,
  CreateReportRequest, PostSortOrder, PostFilter,
  Report,
} from "../../types/community.types";

interface CommunityState {
  posts: PostSummary[];
  selectedPost: PostDetail | null;
  totalCount: number;
  totalPages: number;
  page: number;
  loading: boolean;
  detailLoading: boolean;
  error: string | null;
  groupPosts: PostSummary[];
  groupPostsTotalCount: number;
  groupPostsTotalPages: number;
  groupPostsPage: number;
  groupPostsLoading: boolean;
  groups: Group[];
  selectedGroup: GroupDetail | null;
  groupsTotalCount: number;
  groupsTotalPages: number;
  groupsPage: number;
  groupsLoading: boolean;
  groupDetailLoading: boolean;
  groupsError: string | null;
  reports: Report[];
  reportsTotalCount: number;
  reportsTotalPages: number;
  reportsPage: number;
  reportsLoading: boolean;
  reportsError: string | null;
}

const initialState: CommunityState = {
  posts: [],
  selectedPost: null,
  totalCount: 0,
  totalPages: 1,
  page: 1,
  loading: false,
  detailLoading: false,
  error: null,
  groupPosts: [],
  groupPostsTotalCount: 0,
  groupPostsTotalPages: 1,
  groupPostsPage: 1,
  groupPostsLoading: false,
  groups: [],
  selectedGroup: null,
  groupsTotalCount: 0,
  groupsTotalPages: 1,
  groupsPage: 1,
  groupsLoading: false,
  groupDetailLoading: false,
  groupsError: null,
  reports: [],
  reportsTotalCount: 0,
  reportsTotalPages: 1,
  reportsPage: 1,
  reportsLoading: false,
  reportsError: null,
};

const findComment = (comments: Comment[], commentId: number): Comment | undefined => {
  for (const comment of comments) {
    if (comment.id === commentId) return comment;
    const nested = findComment(comment.replies ?? [], commentId);
    if (nested) return nested;
  }
  return undefined;
};

const removeComment = (comments: Comment[], commentId: number): Comment[] =>
  comments
    .filter((c) => c.id !== commentId)
    .map((c) => ({ ...c, replies: removeComment(c.replies ?? [], commentId) }));

export const fetchPosts = createAsyncThunk(
  "community/fetchPosts",
  async (
    args: {
      category?: string;
      search?: string;
      page?: number;
      sort?: PostSortOrder;
      filter?: PostFilter;
      tag?: string;
    },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.getPosts(
        args.category, args.search, args.page ?? 1, 10,
        args.sort, args.filter, args.tag
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load posts.");
    }
  }
);

export const searchPostsThunk = createAsyncThunk(
  "community/searchPosts",
  async (
    args: { search?: string; category?: string; groupId?: number; page?: number },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.searchPosts(
        args.search, args.category, args.groupId, args.page ?? 1
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to search posts.");
    }
  }
);

export const fetchPostById = createAsyncThunk(
  "community/fetchById",
  async (postId: number, { rejectWithValue }) => {
    try { return await communityService.getPostById(postId); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Post not found.");
    }
  }
);

export const createPostThunk = createAsyncThunk(
  "community/createPost",
  async (payload: CreatePostRequest, { rejectWithValue }) => {
    try { return await communityService.createPost(payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to create post.");
    }
  }
);

export const updatePostThunk = createAsyncThunk(
  "community/updatePost",
  async (
    { postId, payload }: { postId: number; payload: Partial<CreatePostRequest> },
    { rejectWithValue }
  ) => {
    try { return await communityService.updatePost(postId, payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to update post.");
    }
  }
);

export const deletePostThunk = createAsyncThunk(
  "community/deletePost",
  async (postId: number, { rejectWithValue }) => {
    try { await communityService.deletePost(postId); return postId; }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to delete.");
    }
  }
);

export const votePostThunk = createAsyncThunk(
  "community/votePost",
  async (
    { postId, isUpvote }: { postId: number; isUpvote: boolean },
    { rejectWithValue, getState }
  ) => {
    try {
      const upvoteCount = await communityService.votePost(postId, isUpvote);
      const community = (getState() as { community: CommunityState }).community;
      const post = community.posts.find((p) => p.id === postId)
        ?? community.groupPosts.find((p) => p.id === postId)
        ?? community.selectedPost;
      const prevVote = post?.userVote ?? 0;
      const userVote = prevVote === (isUpvote ? 1 : -1) ? 0 : (isUpvote ? 1 : -1);
      return { postId, upvoteCount, userVote };
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to vote.");
    }
  }
);

export const removePostVoteThunk = createAsyncThunk(
  "community/removePostVote",
  async (postId: number, { rejectWithValue }) => {
    try {
      await communityService.removePostVote(postId);
      return postId;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to remove vote.");
    }
  }
);

export const fetchComments = createAsyncThunk(
  "community/fetchComments",
  async (postId: number, { rejectWithValue }) => {
    try { return await communityService.getComments(postId); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load comments.");
    }
  }
);

export const createCommentThunk = createAsyncThunk(
  "community/createComment",
  async (
    { postId, payload }: { postId: number; payload: CreateCommentRequest },
    { rejectWithValue }
  ) => {
    try { return await communityService.addComment(postId, payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to add comment.");
    }
  }
);

export const updateCommentThunk = createAsyncThunk(
  "community/updateComment",
  async (
    { commentId, content }: { commentId: number; content: string },
    { rejectWithValue }
  ) => {
    try { return await communityService.updateComment(commentId, content); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to update comment.");
    }
  }
);

export const deleteCommentThunk = createAsyncThunk(
  "community/deleteComment",
  async (commentId: number, { rejectWithValue }) => {
    try { await communityService.deleteComment(commentId); return commentId; }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to delete comment.");
    }
  }
);

export const voteCommentThunk = createAsyncThunk(
  "community/voteComment",
  async (
    { commentId, isUpvote }: { commentId: number; isUpvote: boolean },
    { rejectWithValue, getState }
  ) => {
    try {
      const upvoteCount = await communityService.voteComment(commentId, isUpvote);
      const community = (getState() as { community: CommunityState }).community;
      const comment = community.selectedPost
        ? findComment(community.selectedPost.comments, commentId)
        : undefined;
      const prevVote = comment?.userVote ?? 0;
      const userVote = prevVote === (isUpvote ? 1 : -1) ? 0 : (isUpvote ? 1 : -1);
      return { commentId, upvoteCount, userVote };
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to vote.");
    }
  }
);

export const removeCommentVoteThunk = createAsyncThunk(
  "community/removeCommentVote",
  async (commentId: number, { rejectWithValue }) => {
    try {
      await communityService.removeCommentVote(commentId);
      return commentId;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to remove vote.");
    }
  }
);

export const reportPostThunk = createAsyncThunk(
  "community/reportPost",
  async (
    { postId, payload }: { postId: number; payload: CreateReportRequest },
    { rejectWithValue }
  ) => {
    try { await communityService.reportPost(postId, payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to report post.");
    }
  }
);

export const reportCommentThunk = createAsyncThunk(
  "community/reportComment",
  async (
    { commentId, payload }: { commentId: number; payload: CreateReportRequest },
    { rejectWithValue }
  ) => {
    try { await communityService.reportComment(commentId, payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to report comment.");
    }
  }
);

export const pinPostThunk = createAsyncThunk(
  "community/pinPost",
  async (postId: number, { rejectWithValue }) => {
    try { return await communityService.pinPost(postId); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to pin post.");
    }
  }
);

export const unpinPostThunk = createAsyncThunk(
  "community/unpinPost",
  async (postId: number, { rejectWithValue }) => {
    try { return await communityService.unpinPost(postId); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to unpin post.");
    }
  }
);

export const fetchGroups = createAsyncThunk(
  "community/fetchGroups",
  async (
    args: { search?: string; isPublic?: boolean; page?: number },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.getGroups(
        args.search, args.isPublic, args.page ?? 1
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load groups.");
    }
  }
);

export const searchGroupsThunk = createAsyncThunk(
  "community/searchGroups",
  async (
    args: { search?: string; isPublic?: boolean; page?: number },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.searchGroups(
        args.search, args.isPublic, args.page ?? 1
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to search groups.");
    }
  }
);

export const fetchGroup = createAsyncThunk(
  "community/fetchGroup",
  async (groupId: number, { rejectWithValue }) => {
    try { return await communityService.getGroup(groupId); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Group not found.");
    }
  }
);

export const createGroupThunk = createAsyncThunk(
  "community/createGroup",
  async (payload: CreateGroupRequest, { rejectWithValue }) => {
    try { return await communityService.createGroup(payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to create group.");
    }
  }
);

export const updateGroupThunk = createAsyncThunk(
  "community/updateGroup",
  async (
    { groupId, payload }: { groupId: number; payload: UpdateGroupRequest },
    { rejectWithValue }
  ) => {
    try { return await communityService.updateGroup(groupId, payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to update group.");
    }
  }
);

export const deleteGroupThunk = createAsyncThunk(
  "community/deleteGroup",
  async (groupId: number, { rejectWithValue }) => {
    try { await communityService.deleteGroup(groupId); return groupId; }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to delete group.");
    }
  }
);

export const joinGroupThunk = createAsyncThunk(
  "community/joinGroup",
  async (groupId: number, { rejectWithValue }) => {
    try { await communityService.joinGroup(groupId); return groupId; }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to join group.");
    }
  }
);

export const leaveGroupThunk = createAsyncThunk(
  "community/leaveGroup",
  async (groupId: number, { rejectWithValue }) => {
    try { await communityService.leaveGroup(groupId); return groupId; }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to leave group.");
    }
  }
);

export const banUserThunk = createAsyncThunk(
  "community/banUser",
  async (args: { groupId: number; userId: string }, { rejectWithValue }) => {
    try {
      await communityService.banUser(args.groupId, args.userId);
      return args;
    }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to ban user.");
    }
  }
);

export const unbanUserThunk = createAsyncThunk(
  "community/unbanUser",
  async (args: { groupId: number; userId: string }, { rejectWithValue }) => {
    try {
      await communityService.unbanUser(args.groupId, args.userId);
      return args;
    }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to unban user.");
    }
  }
);

export const fetchGroupPosts = createAsyncThunk(
  "community/fetchGroupPosts",
  async (
    args: {
      groupId: number;
      search?: string;
      category?: string;
      sort?: PostSortOrder;
      page?: number;
    },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.getGroupPosts(
        args.groupId, args.search, args.category, args.sort, args.page ?? 1
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load group posts.");
    }
  }
);

export const createGroupPostThunk = createAsyncThunk(
  "community/createGroupPost",
  async (
    { groupId, payload }: { groupId: number; payload: CreatePostRequest },
    { rejectWithValue }
  ) => {
    try { return await communityService.createGroupPost(groupId, payload); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to create post.");
    }
  }
);

export const fetchReports = createAsyncThunk(
  "community/fetchReports",
  async (
    args: { page?: number; pageSize?: number },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.getModerationQueue(
        args.page ?? 1, args.pageSize ?? 10
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load reports.");
    }
  }
);

export const resolveReportThunk = createAsyncThunk(
  "community/resolveReport",
  async (reportId: number, { rejectWithValue }) => {
    try {
      await communityService.resolveReport(reportId);
      return reportId;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to resolve report.");
    }
  }
);

export const dismissReportThunk = createAsyncThunk(
  "community/dismissReport",
  async (reportId: number, { rejectWithValue }) => {
    try {
      await communityService.dismissReport(reportId);
      return reportId;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to dismiss report.");
    }
  }
);

const communitySlice = createSlice({
  name: "community",
  initialState,
  reducers: {
    clearSelectedPost(state) { state.selectedPost = null; },
    clearSelectedGroup(state) { state.selectedGroup = null; },
    clearGroupPosts(state) { state.groupPosts = []; },
    updatePostVote(
      state,
      action: PayloadAction<{ postId: number; upvoteCount: number; userVote: number }>
    ) {
      const updatePost = (post: PostSummary) => {
        if (post.id !== action.payload.postId) return;
        post.upvoteCount = action.payload.upvoteCount;
        post.userVote    = action.payload.userVote;
      };
      state.posts.forEach(updatePost);
      state.groupPosts.forEach(updatePost);
      if (state.selectedPost) updatePost(state.selectedPost);
    },
    updateCommentVote(
      state,
      action: PayloadAction<{ commentId: number; upvoteCount: number; userVote: number }>
    ) {
      if (!state.selectedPost) return;
      const comment = findComment(state.selectedPost.comments, action.payload.commentId);
      if (!comment) return;
      comment.upvoteCount = action.payload.upvoteCount;
      comment.userVote    = action.payload.userVote;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPosts.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchPosts.fulfilled, (s, a) => {
        s.loading    = false;
        s.posts      = a.payload.posts;
        s.totalCount = a.payload.totalCount;
        s.totalPages = a.payload.totalPages;
        s.page       = a.payload.page;
      })
      .addCase(fetchPosts.rejected, (s, a) => {
        s.loading = false;
        s.error   = a.payload as string;
      });

    builder
      .addCase(searchPostsThunk.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(searchPostsThunk.fulfilled, (s, a) => {
        s.loading    = false;
        s.posts      = a.payload.posts;
        s.totalCount = a.payload.totalCount;
        s.totalPages = a.payload.totalPages;
        s.page       = a.payload.page;
      })
      .addCase(searchPostsThunk.rejected, (s, a) => {
        s.loading = false;
        s.error   = a.payload as string;
      });

    builder
      .addCase(fetchPostById.pending, (s) => { s.detailLoading = true; s.error = null; })
      .addCase(fetchPostById.fulfilled, (s, a) => {
        s.detailLoading = false;
        s.selectedPost  = a.payload;
      })
      .addCase(fetchPostById.rejected, (s, a) => {
        s.detailLoading = false;
        s.error         = a.payload as string;
      });

    builder
      .addCase(createPostThunk.fulfilled, (s, a) => {
        s.posts.unshift(a.payload);
        s.totalCount++;
      });

    builder
      .addCase(updatePostThunk.fulfilled, (s, a) => {
        const updated = { ...a.payload, editedAt: new Date().toISOString() };
        const idx = s.posts.findIndex((p) => p.id === updated.id);
        if (idx !== -1) s.posts[idx] = updated;
        if (s.selectedPost?.id === updated.id) s.selectedPost = { ...s.selectedPost, ...updated };
      });

    builder
      .addCase(deletePostThunk.fulfilled, (s, a) => {
        s.posts = s.posts.filter((p) => p.id !== a.payload);
        s.totalCount = Math.max(0, s.totalCount - 1);
      });

    builder
      .addCase(votePostThunk.fulfilled, (s, a) => {
        s.posts = s.posts.map((p) =>
          p.id === a.payload.postId
            ? { ...p, upvoteCount: a.payload.upvoteCount, userVote: a.payload.userVote }
            : p
        );
        s.groupPosts = s.groupPosts.map((p) =>
          p.id === a.payload.postId
            ? { ...p, upvoteCount: a.payload.upvoteCount, userVote: a.payload.userVote }
            : p
        );
        if (s.selectedPost?.id === a.payload.postId) {
          s.selectedPost.upvoteCount = a.payload.upvoteCount;
          s.selectedPost.userVote    = a.payload.userVote;
        }
      });

    builder
      .addCase(removePostVoteThunk.fulfilled, (s, a) => {
        const resetVote = (p: PostSummary) =>
          p.id === a.payload ? { ...p, userVote: 0 } : p;
        s.posts = s.posts.map(resetVote);
        s.groupPosts = s.groupPosts.map(resetVote);
        if (s.selectedPost?.id === a.payload) s.selectedPost.userVote = 0;
      });

    builder
      .addCase(fetchComments.fulfilled, (s, a) => {
        if (s.selectedPost) s.selectedPost.comments = a.payload;
      });

    builder
      .addCase(createCommentThunk.fulfilled, (s, a) => {
        if (!s.selectedPost) return;
        const comment = a.payload;
        if (comment.parentCommentId) {
          const parent = findComment(s.selectedPost.comments, comment.parentCommentId);
          if (parent) parent.replies.push(comment);
        } else {
          s.selectedPost.comments.unshift(comment);
        }
        s.selectedPost.commentCount++;
      });

    builder
      .addCase(updateCommentThunk.fulfilled, (s, a) => {
        if (!s.selectedPost) return;
        const comment = findComment(s.selectedPost.comments, a.payload.id);
        if (comment) {
          Object.assign(comment, a.payload, { editedAt: new Date().toISOString() });
        }
      });

    builder
      .addCase(deleteCommentThunk.fulfilled, (s, a) => {
        if (!s.selectedPost) return;
        s.selectedPost.comments = removeComment(s.selectedPost.comments, a.payload);
        s.selectedPost.commentCount = Math.max(0, s.selectedPost.commentCount - 1);
      });

    builder
      .addCase(voteCommentThunk.fulfilled, (s, a) => {
        if (!s.selectedPost) return;
        const comment = findComment(s.selectedPost.comments, a.payload.commentId);
        if (comment) {
          comment.upvoteCount = a.payload.upvoteCount;
          comment.userVote    = a.payload.userVote;
        }
      });

    builder
      .addCase(removeCommentVoteThunk.fulfilled, (s, a) => {
        if (!s.selectedPost) return;
        const comment = findComment(s.selectedPost.comments, a.payload);
        if (comment) comment.userVote = 0;
      });

    builder
      .addCase(pinPostThunk.fulfilled, (s, a) => {
        const pin = (p: PostSummary) => (p.id === a.payload.id ? { ...p, isPinned: true } : p);
        s.posts = s.posts.map(pin);
        s.groupPosts = s.groupPosts.map(pin);
        if (s.selectedPost?.id === a.payload.id) s.selectedPost.isPinned = true;
      });

    builder
      .addCase(unpinPostThunk.fulfilled, (s, a) => {
        const unpin = (p: PostSummary) => (p.id === a.payload.id ? { ...p, isPinned: false } : p);
        s.posts = s.posts.map(unpin);
        s.groupPosts = s.groupPosts.map(unpin);
        if (s.selectedPost?.id === a.payload.id) s.selectedPost.isPinned = false;
      });

    builder
      .addCase(fetchGroups.pending, (s) => { s.groupsLoading = true; s.groupsError = null; })
      .addCase(fetchGroups.fulfilled, (s, a) => {
        s.groupsLoading    = false;
        s.groups           = a.payload.groups;
        s.groupsTotalCount = a.payload.totalCount;
        s.groupsTotalPages = a.payload.totalPages;
        s.groupsPage       = a.payload.page;
      })
      .addCase(fetchGroups.rejected, (s, a) => {
        s.groupsLoading = false;
        s.groupsError   = a.payload as string;
      });

    builder
      .addCase(searchGroupsThunk.fulfilled, (s, a) => {
        s.groups           = a.payload.groups;
        s.groupsTotalCount = a.payload.totalCount;
        s.groupsTotalPages = a.payload.totalPages;
        s.groupsPage       = a.payload.page;
      });

    builder
      .addCase(fetchGroup.pending, (s) => { s.groupDetailLoading = true; s.groupsError = null; })
      .addCase(fetchGroup.fulfilled, (s, a) => {
        s.groupDetailLoading = false;
        s.selectedGroup      = a.payload;
      })
      .addCase(fetchGroup.rejected, (s, a) => {
        s.groupDetailLoading = false;
        s.groupsError        = a.payload as string;
      });

    builder
      .addCase(createGroupThunk.fulfilled, (s, a) => {
        s.groups.unshift(a.payload);
        s.groupsTotalCount++;
      });

    builder
      .addCase(updateGroupThunk.fulfilled, (s, a) => {
        const idx = s.groups.findIndex((g) => g.id === a.payload.id);
        if (idx !== -1) s.groups[idx] = a.payload;
        if (s.selectedGroup?.id === a.payload.id) {
          s.selectedGroup = { ...s.selectedGroup, ...a.payload };
        }
      });

    builder
      .addCase(deleteGroupThunk.fulfilled, (s, a) => {
        s.groups = s.groups.filter((g) => g.id !== a.payload);
        s.groupsTotalCount = Math.max(0, s.groupsTotalCount - 1);
      });

    builder
      .addCase(joinGroupThunk.fulfilled, (s, a) => {
        const group = s.groups.find((g) => g.id === a.payload);
        if (group) group.memberCount++;
        if (s.selectedGroup?.id === a.payload) {
          s.selectedGroup.memberCount++;
          s.selectedGroup.currentUserRole = "Member";
        }
      });

    builder
      .addCase(leaveGroupThunk.fulfilled, (s, a) => {
        const group = s.groups.find((g) => g.id === a.payload);
        if (group) group.memberCount = Math.max(0, group.memberCount - 1);
        if (s.selectedGroup?.id === a.payload) {
          s.selectedGroup.memberCount = Math.max(0, s.selectedGroup.memberCount - 1);
          s.selectedGroup.currentUserRole = null;
        }
      });

    builder
      .addCase(banUserThunk.fulfilled, (s, a) => {
        const member = s.selectedGroup?.members.find((m) => m.userId === a.payload.userId);
        if (member) member.role = "Banned";
      });

    builder
      .addCase(unbanUserThunk.fulfilled, (s, a) => {
        const member = s.selectedGroup?.members.find((m) => m.userId === a.payload.userId);
        if (member) member.role = "Member";
      });

    builder
      .addCase(fetchGroupPosts.pending, (s) => { s.groupPostsLoading = true; s.groupsError = null; })
      .addCase(fetchGroupPosts.fulfilled, (s, a) => {
        s.groupPostsLoading      = false;
        s.groupPosts             = a.payload.posts;
        s.groupPostsTotalCount   = a.payload.totalCount;
        s.groupPostsTotalPages   = a.payload.totalPages;
        s.groupPostsPage         = a.payload.page;
      })
      .addCase(fetchGroupPosts.rejected, (s, a) => {
        s.groupPostsLoading = false;
        s.groupsError       = a.payload as string;
      });

    builder
      .addCase(createGroupPostThunk.fulfilled, (s, a) => {
        s.groupPosts.unshift(a.payload);
        s.groupPostsTotalCount++;
      });

    builder
      .addCase(fetchReports.pending, (s) => { s.reportsLoading = true; s.reportsError = null; })
      .addCase(fetchReports.fulfilled, (s, a) => {
        s.reportsLoading      = false;
        s.reports             = a.payload.reports;
        s.reportsTotalCount   = a.payload.totalCount;
        s.reportsTotalPages   = a.payload.totalPages;
        s.reportsPage         = a.payload.page;
      })
      .addCase(fetchReports.rejected, (s, a) => {
        s.reportsLoading = false;
        s.reportsError   = a.payload as string;
      });

    builder
      .addCase(resolveReportThunk.fulfilled, (s, a) => {
        s.reports = s.reports.filter((r) => r.id !== a.payload);
        s.reportsTotalCount = Math.max(0, s.reportsTotalCount - 1);
      });

    builder
      .addCase(dismissReportThunk.fulfilled, (s, a) => {
        s.reports = s.reports.filter((r) => r.id !== a.payload);
        s.reportsTotalCount = Math.max(0, s.reportsTotalCount - 1);
      });
  },
});

export const {
  clearSelectedPost, clearSelectedGroup, clearGroupPosts,
  updatePostVote, updateCommentVote,
} = communitySlice.actions;
export default communitySlice.reducer;
