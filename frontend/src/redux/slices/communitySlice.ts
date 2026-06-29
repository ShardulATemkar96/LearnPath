import { createAsyncThunk, createSlice, PayloadAction } from "@reduxjs/toolkit";
import { communityService } from "../../services/communityService";
import {
  PostSummary, PostDetail, CreatePostRequest,
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
};

export const fetchPosts = createAsyncThunk(
  "community/fetchPosts",
  async (
    args: { category?: string; search?: string; page?: number },
    { rejectWithValue }
  ) => {
    try {
      return await communityService.getPosts(
        args.category, args.search, args.page ?? 1
      );
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load posts.");
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

export const deletePostThunk = createAsyncThunk(
  "community/deletePost",
  async (postId: number, { rejectWithValue }) => {
    try { await communityService.deletePost(postId); return postId; }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to delete.");
    }
  }
);

const communitySlice = createSlice({
  name: "community",
  initialState,
  reducers: {
    clearSelectedPost(state) { state.selectedPost = null; },
    updatePostVote(
      state,
      action: PayloadAction<{ postId: number; upvoteCount: number; userVote: number }>
    ) {
      const post = state.posts.find((p) => p.id === action.payload.postId);
      if (post) {
        post.upvoteCount = action.payload.upvoteCount;
        post.userVote    = action.payload.userVote;
      }
      if (state.selectedPost?.id === action.payload.postId) {
        state.selectedPost.upvoteCount = action.payload.upvoteCount;
        state.selectedPost.userVote    = action.payload.userVote;
      }
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
      .addCase(deletePostThunk.fulfilled, (s, a) => {
        s.posts = s.posts.filter((p) => p.id !== a.payload);
        s.totalCount = Math.max(0, s.totalCount - 1);
      });
  },
});

export const { clearSelectedPost, updatePostVote } = communitySlice.actions;
export default communitySlice.reducer;
