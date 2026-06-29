import apiClient from "./apiClient";
import {
  PostListResponse, PostDetail,
  PostSummary, Comment,
  CreatePostRequest, CreateCommentRequest,
} from "../types/community.types";

export const communityService = {
  getPosts: async (
    category?: string,
    search?: string,
    page = 1,
    pageSize = 10
  ): Promise<PostListResponse> => {
    const { data } = await apiClient.get("/community/posts", {
      params: { category, search, page, pageSize },
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
};
