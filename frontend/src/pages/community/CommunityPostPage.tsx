import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, CircularProgress,
  Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle,
  Divider, IconButton, Skeleton, Snackbar, Stack,
  TextField, Tooltip, Typography,
} from "@mui/material";
import {
  ArrowBackRounded, ThumbUpRounded,
  ThumbDownRounded, LockRounded,
  EditRounded, DeleteRounded, PushPinRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchPostById, clearSelectedPost, votePostThunk,
  createCommentThunk, deletePostThunk,
} from "../../redux/slices/communitySlice";
import {
  selectSelectedPost, selectPostDetailLoading,
  selectCommunityError,
} from "../../redux/selectors/communitySelectors";
import CommentItem from "../../components/community/CommentItem/CommentItem";
import CreatePostModal from "../../components/community/CreatePostModal/CreatePostModal";
import CodeBlock from "../../components/community/CodeBlock/CodeBlock";
import { dateUtils } from "../../utils/dateUtils";
import { useAuth } from "../../hooks/useAuth";

const CommunityPostPage = () => {
  const { id }     = useParams<{ id: string }>();
  const dispatch   = useDispatch<AppDispatch>();
  const navigate   = useNavigate();
  const { user, isAdmin } = useAuth();

  const post    = useSelector(selectSelectedPost);
  const loading = useSelector(selectPostDetailLoading);
  const error   = useSelector(selectCommunityError);

  const [newComment, setNewComment]   = useState("");
  const [submitting, setSubmitting]   = useState(false);
  const [commentError, setCommentError] = useState("");
  const [editOpen,    setEditOpen]    = useState(false);
  const [deleteOpen,  setDeleteOpen]  = useState(false);
  const [deleting,    setDeleting]    = useState(false);
  const [toast,       setToast]       = useState("");

  const notify = (msg: string) => setToast(msg);

  useEffect(() => {
    if (id) dispatch(fetchPostById(Number(id)));
    return () => { dispatch(clearSelectedPost()); };
  }, [id, dispatch]);

  const handleVotePost = async (isUpvote: boolean) => {
    if (!post) return;
    const result = await dispatch(votePostThunk({ postId: post.id, isUpvote }));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      notify("Vote updated.");
    }
  };

  const handleAddComment = async () => {
    if (!newComment.trim()) {
      setCommentError("Comment cannot be empty."); return;
    }
    if (!post) return;
    setSubmitting(true); setCommentError("");
    const result = await dispatch(createCommentThunk({
      postId: post.id, payload: { content: newComment },
    }));
    setSubmitting(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setNewComment("");
      notify("Comment added.");
    } else {
      setCommentError((result as any).payload ?? "Failed to post comment.");
    }
  };

  const handleDeletePost = async () => {
    if (!post) return;
    setDeleting(true);
    const result = await dispatch(deletePostThunk(post.id));
    setDeleting(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setDeleteOpen(false);
      notify("Post deleted.");
      navigate(-1);
    }
  };

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

  const canModerate = post.authorId === user?.userId || isAdmin;

  return (
    <Box>
      <Button
        startIcon={<ArrowBackRounded />}
        onClick={() => navigate(-1)}
        sx={{ mb: 3, color: "text.secondary" }}
      >
        Back
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
            {post.isPinned && (
              <Tooltip title="Pinned">
                <Stack direction="row" alignItems="center" spacing={0.5}>
                  <PushPinRounded sx={{ fontSize: 15, color: "primary.main" }} />
                  <Typography variant="caption" color="text.secondary">
                    Pinned
                  </Typography>
                </Stack>
              </Tooltip>
            )}
            {post.isLocked && (
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <LockRounded sx={{ fontSize: 15, color: "text.disabled" }} />
                <Typography variant="caption" color="text.secondary">
                  Locked
                </Typography>
              </Stack>
            )}
            <Stack direction="row" alignItems="center" spacing={1} ml="auto">
              <Typography variant="caption" color="text.secondary">
                by {post.authorIsDeleted ? "Deleted User" : post.authorName} ·{" "}
                {post.editedAt
                  ? <>Edited · {dateUtils.timeAgo(post.editedAt)}</>
                  : dateUtils.format(post.createdAt)}
              </Typography>
              {post.authorIsDeleted && (
                <Chip
                  label="Deleted"
                  size="small"
                  color="error"
                  sx={{ height: 18, fontSize: "0.6rem", fontWeight: 700 }}
                />
              )}
            </Stack>
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

          {post.tags && (
            <Stack direction="row" spacing={1} flexWrap="wrap" gap={0.5}>
              {post.tags.split(",").map((tag) => tag.trim()).filter(Boolean).map((tag) => (
                <Chip
                  key={tag}
                  label={tag}
                  size="small"
                  variant="outlined"
                  sx={{ fontSize: "0.7rem", fontWeight: 600 }}
                />
              ))}
            </Stack>
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

          {post.codeSnippet && (
            <CodeBlock
              code={post.codeSnippet}
              language={post.programmingLanguage}
            />
          )}

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
              {post.viewCount} views · {post.commentCount} comments
            </Typography>

            {canModerate && (
              <>
                <IconButton
                  onClick={() => setEditOpen(true)}
                  sx={{ ml: "auto", color: "text.secondary" }}
                >
                  <EditRounded />
                </IconButton>
                <Tooltip title="Delete">
                  <IconButton
                    onClick={() => setDeleteOpen(true)}
                    sx={{ color: "error.main" }}
                  >
                    <DeleteRounded />
                  </IconButton>
                </Tooltip>
              </>
            )}
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
        Comments ({post.commentCount})
      </Typography>

      {post.comments.length === 0 ? (
        <Box sx={{ py: 5, textAlign: "center" }}>
          <Typography variant="body2" color="text.secondary">
            No comments yet. Be the first!
          </Typography>
        </Box>
      ) : (
        <Stack spacing={2.5}>
          {post.comments.map((c) => (
            <CommentItem
              key={c.id}
              comment={c}
              postId={post.id}
              onNotify={notify}
            />
          ))}
        </Stack>
      )}

      <CreatePostModal
        open={editOpen}
        onClose={() => setEditOpen(false)}
        post={post}
        onSaved={() => notify("Post updated.")}
      />

      <Dialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        maxWidth="xs" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}
      >
        <DialogTitle sx={{ pb: 1 }}>Delete Post</DialogTitle>
        <DialogContent>
          <DialogContentText>
            This will permanently delete this post. This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button
            onClick={() => setDeleteOpen(false)}
            disabled={deleting}
            sx={{ borderRadius: 2 }}
          >
            Cancel
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleDeletePost}
            disabled={deleting}
            sx={{ borderRadius: 2 }}
          >
            {deleting
              ? <CircularProgress size={20} />
              : "Delete"}
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={!!toast}
        autoHideDuration={3000}
        onClose={() => setToast("")}
        message={toast}
      />
    </Box>
  );
};

export default CommunityPostPage;
