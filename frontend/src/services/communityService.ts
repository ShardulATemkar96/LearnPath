import apiClient from "./apiClient";
import {
  PostListResponse, PostDetail,
  PostSummary, Comment,
  CreatePostRequest, CreateCommentRequest,
  Group, GroupDetail, GroupListResponse,
  CreateGroupRequest, UpdateGroupRequest,
  CreateReportRequest, PostSortOrder, PostFilter,
  ReportListResponse,
} from "../types/community.types";

export const communityService = {
  getPosts: async (
    category?: string,
    search?: string,
    page = 1,
    pageSize = 10,
    sort?: PostSortOrder,
    filter?: PostFilter,
    tag?: string
  ): Promise<PostListResponse> => {
    const FILTER_PARAM: Record<PostFilter, string> = {
      "All Posts":  "All",
      "My Posts":   "Mine",
      "Pinned":     "Pinned",
      "With Code":  "WithCode",
      "Without Code": "WithoutCode",
    };
    const { data } = await apiClient.get("/community/posts", {
      params: {
        category, search, page, pageSize, sort,
        filter: filter ? FILTER_PARAM[filter] : undefined,
        tag,
      },
    });
    return data.data;
  },

  getPostById: async (postId: number): Promise<PostDetail> => {
    const { data } = await apiClient.get(`/community/posts/${postId}`);
    return data.data;
  },

  createPost: async (payload: CreatePostRequest): Promise<PostSummary> => {
    const { data } = await apiClient.post("/community/posts", payload);
    return data.data;
  },

  updatePost: async (
    postId: number,
    payload: Partial<CreatePostRequest>
  ): Promise<PostSummary> => {
    const { data } = await apiClient.put(`/community/posts/${postId}`, payload);
    return data.data;
  },

  deletePost: async (postId: number): Promise<void> => {
    await apiClient.delete(`/community/posts/${postId}`);
  },

  votePost: async (postId: number, isUpvote: boolean): Promise<number> => {
    const { data } = await apiClient.post(
      `/community/posts/${postId}/vote`, { isUpvote }
    );
    return data.data;
  },

  removePostVote: async (postId: number): Promise<void> => {
    await apiClient.delete(`/community/posts/${postId}/vote`);
  },

  getComments: async (postId: number): Promise<Comment[]> => {
    const { data } = await apiClient.get(`/community/posts/${postId}/comments`);
    return data.data;
  },

  addComment: async (
    postId: number,
    payload: CreateCommentRequest
  ): Promise<Comment> => {
    const { data } = await apiClient.post(
      `/community/posts/${postId}/comments`, payload
    );
    return data.data;
  },

  updateComment: async (
    commentId: number,
    content: string
  ): Promise<Comment> => {
    const { data } = await apiClient.put(
      `/community/comments/${commentId}`, { content }
    );
    return data.data;
  },

  deleteComment: async (commentId: number): Promise<void> => {
    await apiClient.delete(`/community/comments/${commentId}`);
  },

  voteComment: async (
    commentId: number,
    isUpvote: boolean
  ): Promise<number> => {
    const { data } = await apiClient.post(
      `/community/comments/${commentId}/vote`, { isUpvote }
    );
    return data.data;
  },

  removeCommentVote: async (commentId: number): Promise<void> => {
    await apiClient.delete(`/community/comments/${commentId}/vote`);
  },

  reportPost: async (
    postId: number,
    payload: CreateReportRequest
  ): Promise<void> => {
    await apiClient.post(`/community/posts/${postId}/report`, payload);
  },

  reportComment: async (
    commentId: number,
    payload: CreateReportRequest
  ): Promise<void> => {
    await apiClient.post(`/community/comments/${commentId}/report`, payload);
  },

  pinPost: async (postId: number): Promise<PostSummary> => {
    const { data } = await apiClient.post(`/community/posts/${postId}/pin`);
    return data.data;
  },

  unpinPost: async (postId: number): Promise<PostSummary> => {
    const { data } = await apiClient.delete(`/community/posts/${postId}/pin`);
    return data.data;
  },

  searchPosts: async (
    search?: string,
    category?: string,
    groupId?: number,
    page = 1,
    pageSize = 10
  ): Promise<PostListResponse> => {
    const { data } = await apiClient.get("/community/posts/search", {
      params: { search, category, groupId, page, pageSize },
    });
    return data.data;
  },

  getGroups: async (
    search?: string,
    isPublic?: boolean,
    page = 1,
    pageSize = 10
  ): Promise<GroupListResponse> => {
    const { data } = await apiClient.get("/community/groups", {
      params: { search, isPublic, page, pageSize },
    });
    return data.data;
  },

  searchGroups: async (
    search?: string,
    isPublic?: boolean,
    page = 1,
    pageSize = 10
  ): Promise<GroupListResponse> => {
    const { data } = await apiClient.get("/community/groups/search", {
      params: { search, isPublic, page, pageSize },
    });
    return data.data;
  },

  getGroup: async (groupId: number): Promise<GroupDetail> => {
    const { data } = await apiClient.get(`/community/groups/${groupId}`);
    return data.data;
  },

  createGroup: async (payload: CreateGroupRequest): Promise<Group> => {
    const { data } = await apiClient.post("/community/groups", payload);
    return data.data;
  },

  updateGroup: async (
    groupId: number,
    payload: UpdateGroupRequest
  ): Promise<Group> => {
    const { data } = await apiClient.put(`/community/groups/${groupId}`, payload);
    return data.data;
  },

  deleteGroup: async (groupId: number): Promise<void> => {
    await apiClient.delete(`/community/groups/${groupId}`);
  },

  joinGroup: async (groupId: number): Promise<void> => {
    await apiClient.post(`/community/groups/${groupId}/join`);
  },

  leaveGroup: async (groupId: number): Promise<void> => {
    await apiClient.post(`/community/groups/${groupId}/leave`);
  },

  banUser: async (groupId: number, userId: string): Promise<void> => {
    await apiClient.post(`/community/groups/${groupId}/members/${userId}/ban`);
  },

  unbanUser: async (groupId: number, userId: string): Promise<void> => {
    await apiClient.post(`/community/groups/${groupId}/members/${userId}/unban`);
  },

  getGroupPosts: async (
    groupId: number,
    search?: string,
    category?: string,
    sort?: PostSortOrder,
    page = 1,
    pageSize = 10
  ): Promise<PostListResponse> => {
    const { data } = await apiClient.get(`/community/groups/${groupId}/posts`, {
      params: { search, category, sort, page, pageSize },
    });
    return data.data;
  },

  createGroupPost: async (
    groupId: number,
    payload: CreatePostRequest
  ): Promise<PostSummary> => {
    const { data } = await apiClient.post(
      `/community/groups/${groupId}/posts`, payload
    );
    return data.data;
  },

  getModerationQueue: async (
    page = 1,
    pageSize = 10
  ): Promise<ReportListResponse> => {
    const { data } = await apiClient.get("/community/reports", {
      params: { page, pageSize },
    });
    return data.data;
  },

  resolveReport: async (reportId: number): Promise<void> => {
    await apiClient.post(`/community/reports/${reportId}/resolve`);
  },

  dismissReport: async (reportId: number): Promise<void> => {
    await apiClient.post(`/community/reports/${reportId}/dismiss`);
  },
};
