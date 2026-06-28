import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, CircularProgress,
  Divider, IconButton, Skeleton, Stack,
  TextField, Tooltip, Typography,
} from "@mui/material";
import {
  ArrowBackRounded, ThumbUpRounded,
  ThumbDownRounded, LockRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchPostById, clearSelectedPost, updatePostVote,
} from "../../redux/slices/communitySlice";
import {
  selectSelectedPost, selectPostDetailLoading,
  selectCommunityError,
} from "../../redux/selectors/communitySelectors";
import { communityService } from "../../services/communityService";
import { Comment } from "../../types/community.types";
import CommentItem from "../../components/community/CommentItem/CommentItem";
import { dateUtils } from "../../utils/dateUtils";
import { useAuth } from "../../hooks/useAuth";
import { ROUTES } from "../../constants/routes";

const CommunityPostPage = () => {
  const { id }     = useParams<{ id: string }>();
  const dispatch   = useDispatch<AppDispatch>();
  const navigate   = useNavigate();
  const { user }   = useAuth();

  const post    = useSelector(selectSelectedPost);
  const loading = useSelector(selectPostDetailLoading);
  const error   = useSelector(selectCommunityError);

  const [comments, setComments]   = useState<Comment[]>([]);
  const [newComment, setNewComment] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [commentError, setCommentError] = useState("");

  useEffect(() => {
    if (id) dispatch(fetchPostById(Number(id)));
    return () => { dispatch(clearSelectedPost()); };
  }, [id, dispatch]);

  useEffect(() => {
    if (post?.comments) setComments(post.comments);
  }, [post]);

  const handleVotePost = async (isUpvote: boolean) => {
    if (!post) return;
    try {
      const newCount = await communityService.votePost(post.id, isUpvote);
      const newVote  =
        post.userVote === (isUpvote ? 1 : -1) ? 0 : (isUpvote ? 1 : -1);
      dispatch(updatePostVote({
        postId: post.id, upvoteCount: newCount, userVote: newVote,
      }));
    } catch { /* silent */ }
  };

  const handleAddComment = async () => {
    if (!newComment.trim()) {
      setCommentError("Comment cannot be empty."); return;
    }
    if (!post) return;
    setSubmitting(true); setCommentError("");
    try {
      const added = await communityService.addComment(post.id, {
        content: newComment,
      });
      setComments((prev) => [added, ...prev]);
      setNewComment("");
    } catch (e: any) {
      setCommentError(e.response?.data?.message ?? "Failed to post comment.");
    } finally { setSubmitting(false); }
  };

  const handleReplyAdded = useCallback(
    (reply: Comment, parentId: number) => {
      setComments((prev) =>
        prev.map((c) =>
          c.id === parentId
            ? { ...c, replies: [...(c.replies ?? []), reply] }
            : c
        )
      );
    },
    []
  );

  const handleCommentDeleted = useCallback((commentId: number) => {
    setComments((prev) => {
      const removeDeep = (list: Comment[]): Comment[] =>
        list
          .filter((c) => c.id !== commentId)
          .map((c) => ({ ...c, replies: removeDeep(c.replies ?? []) }));
      return removeDeep(prev);
    });
  }, []);

  if (loading) return (
    <Box>
      <Skeleton height={40} width={180} sx={{ mb: 2 }} />
      <Skeleton variant="rounded" height={240} sx={{ borderRadius: 4, mb: 3 }} />
      <Skeleton variant="rounded" height={100} sx={{ borderRadius: 3, mb: 2 }} />
      <Skeleton variant="rounded" height={100} sx={{ borderRadius: 3 }} />
    </Box>
  );

  if (error) return (
    <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>
  );

  if (!post) return null;

  return (
    <Box>
      <Button
        startIcon={<ArrowBackRounded />}
        onClick={() => navigate(ROUTES.COMMUNITY)}
        sx={{ mb: 3, color: "text.secondary" }}
      >
        Community
      </Button>

      <Box sx={{
        p: { xs: 3, md: 4 }, borderRadius: 4, mb: 4,
        bgcolor: "background.paper",
        boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
        border: "1.5px solid", borderColor: "divider",
      }}>
        <Stack spacing={2.5}>
          <Stack direction="row" alignItems="center"
            spacing={1.5} flexWrap="wrap">
            <Chip
              label={post.category}
              size="small"
              sx={{ fontWeight: 600, fontSize: "0.72rem" }}
            />
            {post.isLocked && (
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <LockRounded sx={{ fontSize: 15, color: "text.disabled" }} />
                <Typography variant="caption" color="text.secondary">
                  Locked
                </Typography>
              </Stack>
            )}
            <Typography variant="caption" color="text.secondary" ml="auto">
              by {post.authorName} · {dateUtils.format(post.createdAt)}
            </Typography>
          </Stack>

          <Typography variant="h4" fontWeight={700} lineHeight={1.3}>
            {post.title}
          </Typography>

          {post.learningPathTitle && (
            <Chip
              label={`Path: ${post.learningPathTitle}`}
              size="small"
              variant="outlined"
              color="primary"
              sx={{ alignSelf: "flex-start", fontSize: "0.72rem" }}
            />
          )}

          <Divider />

          <Typography
            variant="body1"
            color="text.primary"
            lineHeight={1.8}
            sx={{ whiteSpace: "pre-wrap" }}
          >
            {post.content}
          </Typography>

          <Divider />

          <Stack direction="row" alignItems="center" spacing={1}>
            <IconButton
              onClick={() => handleVotePost(true)}
              sx={{ color: post.userVote === 1 ? "primary.main" : "text.secondary" }}
            >
              <ThumbUpRounded />
            </IconButton>
            <Typography fontWeight={700} color="primary.main">
              {post.upvoteCount}
            </Typography>
            <IconButton
              onClick={() => handleVotePost(false)}
              sx={{ color: post.userVote === -1 ? "error.main" : "text.secondary" }}
            >
              <ThumbDownRounded />
            </IconButton>
            <Typography variant="body2" color="text.secondary" ml={2}>
              {post.viewCount} views · {comments.length} comments
            </Typography>
          </Stack>
        </Stack>
      </Box>

      {!post.isLocked ? (
        <Box sx={{ mb: 4 }}>
          <Typography variant="h6" fontWeight={700} mb={2}>
            Leave a comment
          </Typography>
          {commentError && (
            <Alert severity="error" sx={{ mb: 2, borderRadius: 2 }}>
              {commentError}
            </Alert>
          )}
          <Stack spacing={1.5}>
            <TextField
              fullWidth multiline rows={3}
              placeholder="Share your thoughts..."
              value={newComment}
              onChange={(e) => setNewComment(e.target.value)}
              sx={{ "& .MuiOutlinedInput-root": { borderRadius: 3 } }}
            />
            <Box>
              <Button
                variant="contained"
                onClick={handleAddComment}
                disabled={submitting}
                sx={{
                  background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                  borderRadius: 2,
                }}
              >
                {submitting
                  ? <CircularProgress size={20} sx={{ color: "#fff" }} />
                  : "Post Comment"}
              </Button>
            </Box>
          </Stack>
        </Box>
      ) : (
        <Alert severity="warning" sx={{ mb: 4, borderRadius: 2 }}>
          This post is locked. No new comments allowed.
        </Alert>
      )}

      <Typography variant="h6" fontWeight={700} mb={2}>
        Comments ({comments.length})
      </Typography>

      {comments.length === 0 ? (
        <Box sx={{ py: 5, textAlign: "center" }}>
          <Typography variant="body2" color="text.secondary">
            No comments yet. Be the first!
          </Typography>
        </Box>
      ) : (
        <Stack spacing={2.5}>
          {comments.map((c) => (
            <CommentItem
              key={c.id}
              comment={c}
              postId={post.id}
              onReplyAdded={handleReplyAdded}
              onDeleted={handleCommentDeleted}
            />
          ))}
        </Stack>
      )}
    </Box>
  );
};

export default CommunityPostPage;
