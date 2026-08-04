import { describe, it, expect } from "vitest";
import { configureStore } from "@reduxjs/toolkit";
import communityReducer, {
  clearSelectedPost, clearSelectedGroup, updatePostVote, updateCommentVote,
} from "../../redux/slices/communitySlice";
import type {
  PostSummary, PostDetail, Comment, Group, GroupDetail,
} from "../../types/community.types";

interface StoreOverrides {
  posts?: PostSummary[];
  selectedPost?: PostDetail | null;
  groupPosts?: PostSummary[];
  groups?: Group[];
  selectedGroup?: GroupDetail | null;
}

const makeStore = (overrides: StoreOverrides = {}) =>
  configureStore({
    reducer:        { community: communityReducer },
    preloadedState: {
      community: {
        posts:             overrides.posts ?? [],
        selectedPost:      overrides.selectedPost ?? null,
        totalCount:        overrides.posts?.length ?? 0,
        totalPages:        1,
        page:              1,
        loading:           false,
        detailLoading:     false,
        error:             null,
        groupPosts:             overrides.groupPosts ?? [],
        groupPostsTotalCount:   overrides.groupPosts?.length ?? 0,
        groupPostsTotalPages:   1,
        groupPostsPage:         1,
        groupPostsLoading:      false,
        groups:                 overrides.groups ?? [],
        selectedGroup:          overrides.selectedGroup ?? null,
        groupsTotalCount:       overrides.groups?.length ?? 0,
        groupsTotalPages:       1,
        groupsPage:             1,
        groupsLoading:          false,
        groupDetailLoading:     false,
        groupsError:            null,
        reports:                [],
        reportsTotalCount:      0,
        reportsTotalPages:      1,
        reportsPage:            1,
        reportsLoading:         false,
        reportsError:           null,
      },
    },
  });

const mockPost: PostSummary = {
  id:                1,
  title:             "Test Post",
  contentPreview:    "Preview...",
  authorId:          "author-1",
  authorName:        "Author Name",
  authorIsDeleted:   false,
  category:          "General",
  isPinned:          false,
  isLocked:          false,
  viewCount:         10,
  upvoteCount:       5,
  commentCount:      2,
  userVote:          0,
  createdAt:         new Date().toISOString(),
};

const mockComment: Comment = {
  id:               10,
  content:          "A comment",
  authorId:         "author-2",
  authorName:       "Another Author",
  authorIsDeleted:  false,
  postId:           1,
  upvoteCount:      3,
  userVote:         0,
  replies:          [],
  createdAt:        new Date().toISOString(),
  updatedAt:        new Date().toISOString(),
};

const mockPostDetail: PostDetail = {
  ...mockPost,
  content:  "Full content",
  comments: [mockComment],
};

const mockGroupDetail: GroupDetail = {
  id:              1,
  name:            "Test Group",
  description:     "A group",
  ownerId:         "owner-1",
  ownerName:       "Owner",
  isPublic:        true,
  memberCount:     1,
  postCount:       0,
  createdAt:       new Date().toISOString(),
  members:         [],
  currentUserRole: "Owner",
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

  it("clearSelectedGroup sets selectedGroup to null", () => {
    const store = makeStore({ selectedGroup: mockGroupDetail });
    store.dispatch(clearSelectedGroup());
    expect(store.getState().community.selectedGroup).toBeNull();
  });

  it("updatePostVote updates post vote count in list", () => {
    const store = makeStore({ posts: [mockPost] });
    store.dispatch(updatePostVote({
      postId:      1,
      upvoteCount: 6,
      userVote:    1,
    }));

    const updated = store.getState().community.posts[0];
    expect(updated.upvoteCount).toBe(6);
    expect(updated.userVote).toBe(1);
  });

  it("updatePostVote updates a group post vote", () => {
    const store = makeStore({ groupPosts: [mockPost] });
    store.dispatch(updatePostVote({
      postId:      1,
      upvoteCount: 8,
      userVote:    1,
    }));

    const updated = store.getState().community.groupPosts[0];
    expect(updated.upvoteCount).toBe(8);
    expect(updated.userVote).toBe(1);
  });

  it("updateCommentVote updates a comment inside the selected post", () => {
    const store = makeStore({ selectedPost: mockPostDetail });
    store.dispatch(updateCommentVote({
      commentId:    10,
      upvoteCount:  5,
      userVote:     1,
    }));

    const comment = store.getState().community.selectedPost!.comments[0];
    expect(comment.upvoteCount).toBe(5);
    expect(comment.userVote).toBe(1);
  });
});
