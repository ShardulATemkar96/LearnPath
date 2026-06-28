tsimport { describe, it, expect } from "vitest";
import { configureStore } from "@reduxjs/toolkit";
import communityReducer, {
  clearSelectedPost, updatePostVote,
} from "../../redux/slices/communitySlice";
import type { PostSummary } from "../../types/community.types";

const makeStore = (posts: PostSummary[] = []) =>
  configureStore({
    reducer:        { community: communityReducer },
    preloadedState: {
      community: {
        posts,
        selectedPost:  null,
        totalCount:    posts.length,
        totalPages:    1,
        page:          1,
        loading:       false,
        detailLoading: false,
        error:         null,
      },
    },
  });

const mockPost: PostSummary = {
  id:                1,
  title:             "Test Post",
  contentPreview:    "Preview...",
  authorId:          "author-1",
  authorName:        "Author Name",
  category:          "General",
  isPinned:          false,
  isLocked:          false,
  viewCount:         10,
  upvoteCount:       5,
  commentCount:      2,
  userVote:          0,
  createdAt:         new Date().toISOString(),
};

describe("communitySlice", () => {
  it("initial state is correct", () => {
    const store = makeStore();
    const state = store.getState().community;
    expect(state.posts).toHaveLength(0);
    expect(state.selectedPost).toBeNull();
    expect(state.loading).toBe(false);
  });

  it("clearSelectedPost sets selectedPost to null", () => {
    const store = makeStore();
    store.dispatch(clearSelectedPost());
    expect(store.getState().community.selectedPost).toBeNull();
  });

  it("updatePostVote updates post vote count in list", () => {
    const store = makeStore([mockPost]);
    store.dispatch(updatePostVote({
      postId:      1,
      upvoteCount: 6,
      userVote:    1,
    }));

    const updated = store.getState().community.posts[0];
    expect(updated.upvoteCount).toBe(6);
    expect(updated.userVote).toBe(1);
  });
});
