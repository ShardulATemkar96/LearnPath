import { useDispatch, useSelector } from "react-redux";
import { AppDispatch } from "../redux/store";
import {
  selectPosts, selectSelectedPost, selectCommunityLoading,
  selectPostDetailLoading, selectCommunityError,
  selectTotalPages, selectCurrentPage, selectTotalCount,
} from "../redux/selectors/communitySelectors";
import {
  fetchPosts, searchPostsThunk, fetchPostById,
  createPostThunk, updatePostThunk, deletePostThunk,
  votePostThunk, removePostVoteThunk,
  fetchComments, createCommentThunk, updateCommentThunk, deleteCommentThunk,
  voteCommentThunk, removeCommentVoteThunk,
  reportPostThunk, reportCommentThunk,
  pinPostThunk, unpinPostThunk,
  clearSelectedPost,
} from "../redux/slices/communitySlice";
import {
  CreatePostRequest, CreateCommentRequest, CreateReportRequest,
} from "../types/community.types";

export const useCommunity = () => {
  const dispatch = useDispatch<AppDispatch>();

  const posts = useSelector(selectPosts);
  const selectedPost = useSelector(selectSelectedPost);
  const loading = useSelector(selectCommunityLoading);
  const detailLoading = useSelector(selectPostDetailLoading);
  const error = useSelector(selectCommunityError);
  const totalPages = useSelector(selectTotalPages);
  const page = useSelector(selectCurrentPage);
  const totalCount = useSelector(selectTotalCount);

  const loadPosts = (args?: { category?: string; search?: string; page?: number }) =>
    dispatch(fetchPosts(args ?? {}));
  const searchPosts = (args?: { search?: string; category?: string; groupId?: number; page?: number }) =>
    dispatch(searchPostsThunk(args ?? {}));
  const loadPost = (postId: number) => dispatch(fetchPostById(postId));
  const createPost = (payload: CreatePostRequest) => dispatch(createPostThunk(payload));
  const updatePost = (postId: number, payload: Partial<CreatePostRequest>) =>
    dispatch(updatePostThunk({ postId, payload }));
  const deletePost = (postId: number) => dispatch(deletePostThunk(postId));
  const votePost = (postId: number, isUpvote: boolean) =>
    dispatch(votePostThunk({ postId, isUpvote }));
  const removePostVote = (postId: number) => dispatch(removePostVoteThunk(postId));
  const loadComments = (postId: number) => dispatch(fetchComments(postId));
  const createComment = (postId: number, payload: CreateCommentRequest) =>
    dispatch(createCommentThunk({ postId, payload }));
  const updateComment = (commentId: number, content: string) =>
    dispatch(updateCommentThunk({ commentId, content }));
  const deleteComment = (commentId: number) => dispatch(deleteCommentThunk(commentId));
  const voteComment = (commentId: number, isUpvote: boolean) =>
    dispatch(voteCommentThunk({ commentId, isUpvote }));
  const removeCommentVote = (commentId: number) => dispatch(removeCommentVoteThunk(commentId));
  const reportPost = (postId: number, payload: CreateReportRequest) =>
    dispatch(reportPostThunk({ postId, payload }));
  const reportComment = (commentId: number, payload: CreateReportRequest) =>
    dispatch(reportCommentThunk({ commentId, payload }));
  const pinPost = (postId: number) => dispatch(pinPostThunk(postId));
  const unpinPost = (postId: number) => dispatch(unpinPostThunk(postId));

  return {
    posts, selectedPost, loading, detailLoading, error, totalPages, page, totalCount,
    loadPosts, searchPosts, loadPost,
    createPost, updatePost, deletePost,
    votePost, removePostVote,
    loadComments, createComment, updateComment, deleteComment,
    voteComment, removeCommentVote,
    reportPost, reportComment,
    pinPost, unpinPost,
    clearSelectedPost: () => dispatch(clearSelectedPost()),
  };
};
