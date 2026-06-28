tsimport { RootState } from "../store";

export const selectPosts            = (s: RootState) => s.community.posts;
export const selectSelectedPost     = (s: RootState) => s.community.selectedPost;
export const selectCommunityLoading = (s: RootState) => s.community.loading;
export const selectPostDetailLoading = (s: RootState) => s.community.detailLoading;
export const selectCommunityError   = (s: RootState) => s.community.error;
export const selectTotalPages       = (s: RootState) => s.community.totalPages;
export const selectCurrentPage      = (s: RootState) => s.community.page;
export const selectTotalCount       = (s: RootState) => s.community.totalCount;
