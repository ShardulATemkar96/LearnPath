import { useState } from "react";
import {
  Avatar, Box, Button, IconButton,
  Stack, TextField, Tooltip, Typography,
} from "@mui/material";
import {
  ThumbUpRounded, ReplyRounded,
  ThumbDownRounded, DeleteRounded, EditRounded, FlagRounded,
  ExpandMoreRounded, ExpandLessRounded,
} from "@mui/icons-material";
import { useDispatch } from "react-redux";
import { Comment } from "../../../types/community.types";
import {
  voteCommentThunk, updateCommentThunk,
  deleteCommentThunk, createCommentThunk,
} from "../../../redux/slices/communitySlice";
import { AppDispatch } from "../../../redux/store";
import { dateUtils } from "../../../utils/dateUtils";
import { useAuth } from "../../../hooks/useAuth";
import ReportDialog from "../ReportDialog/ReportDialog";

interface CommentItemProps {
  comment: Comment;
  postId: number;
  onNotify: (msg: string) => void;
  depth?: number;
}

const CommentItem = ({
  comment, postId, onNotify, depth = 0,
}: CommentItemProps) => {
  const { user, isAdmin } = useAuth();
  const dispatch = useDispatch<AppDispatch>();

  const [replying,  setReplying]  = useState(false);
  const [replyText, setReplyText] = useState("");
  const [editing,   setEditing]   = useState(false);
  const [editText,  setEditText]  = useState(comment.content);
  const [collapsed, setCollapsed] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);

  const canModerate = comment.authorId === user?.userId || isAdmin;
  const hasReplies  = (comment.replies?.length ?? 0) > 0;

  const handleVote = async (isUpvote: boolean) => {
    const result = await dispatch(voteCommentThunk({
      commentId: comment.id, isUpvote,
    }));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      onNotify("Vote updated.");
    }
  };

  const handleReply = async () => {
    if (!replyText.trim()) return;
    setSubmitting(true);
    const result = await dispatch(createCommentThunk({
      postId,
      payload: { content: replyText, parentCommentId: comment.id },
    }));
    setSubmitting(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setReplyText("");
      setReplying(false);
      onNotify("Reply added.");
    }
  };

  const handleEditSave = async () => {
    if (!editText.trim()) return;
    setSubmitting(true);
    const result = await dispatch(updateCommentThunk({
      commentId: comment.id, content: editText,
    }));
    setSubmitting(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setEditing(false);
      onNotify("Comment updated.");
    }
  };

  const handleDelete = async () => {
    if (!window.confirm("Delete this comment?")) return;
    const result = await dispatch(deleteCommentThunk(comment.id));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      onNotify("Comment deleted.");
    }
  };

  return (
    <Box
      sx={{
        pl: depth > 0 ? 3 : 0,
        borderLeft: depth > 0
          ? "2px solid rgba(108,99,255,0.15)" : "none",
        ml: depth > 0 ? 1 : 0,
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <Avatar
            sx={{
              width: 34, height: 34, fontSize: "0.8rem",
              fontWeight: 700, flexShrink: 0,
              background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
            }}
          >
            {comment.authorName.split(" ").map((n) => n[0]).join("").slice(0, 2)}
          </Avatar>

          <Box
            sx={{
              flexGrow: 1, p: 2, borderRadius: 3,
              bgcolor: "rgba(108,99,255,0.03)",
              border: "1px solid", borderColor: "divider",
            }}
          >
            <Stack direction="row" justifyContent="space-between"
              alignItems="center" mb={0.75}>
              <Stack direction="row" alignItems="center" spacing={1}>
                <Stack direction="row" alignItems="center" spacing={0.5}>
                  <Typography variant="body2" fontWeight={700}>
                    {comment.authorIsDeleted ? "Deleted User" : comment.authorName}
                  </Typography>
                  {comment.authorIsDeleted && (
                    <Box
                      component="span"
                      sx={{
                        fontSize: "0.6rem", fontWeight: 700, color: "#fff",
                        bgcolor: "error.main", px: 0.7, py: 0.2, borderRadius: 1,
                      }}
                    >
                      Deleted
                    </Box>
                  )}
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  {comment.editedAt
                    ? <>Edited · {dateUtils.timeAgo(comment.editedAt)}</>
                    : dateUtils.timeAgo(comment.createdAt)}
                </Typography>
              </Stack>
              {hasReplies && (
                <Tooltip title={collapsed ? "Show replies" : "Hide replies"}>
                  <IconButton
                    size="small"
                    onClick={() => setCollapsed((c) => !c)}
                    sx={{ p: 0.3 }}
                  >
                    {collapsed
                      ? <ExpandMoreRounded sx={{ fontSize: 16 }} />
                      : <ExpandLessRounded sx={{ fontSize: 16 }} />}
                  </IconButton>
                </Tooltip>
              )}
            </Stack>

            {editing ? (
              <Stack spacing={1}>
                <TextField
                  size="small" fullWidth multiline rows={3}
                  value={editText}
                  onChange={(e) => setEditText(e.target.value)}
                  autoFocus
                  sx={{ "& .MuiOutlinedInput-root": { borderRadius: 2.5 } }}
                />
                <Stack direction="row" spacing={1}>
                  <Button
                    size="small" variant="contained"
                    onClick={handleEditSave} disabled={submitting}
                    sx={{
                      borderRadius: 2,
                      background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                    }}
                  >
                    {submitting ? "..." : "Save"}
                  </Button>
                  <Button
                    size="small" variant="outlined"
                    onClick={() => setEditing(false)}
                    sx={{ borderRadius: 2 }}
                  >
                    Cancel
                  </Button>
                </Stack>
              </Stack>
            ) : (
              <Typography variant="body2" color="text.primary" lineHeight={1.6}>
                {comment.content}
              </Typography>
            )}

            {/* Actions */}
            <Stack direction="row" alignItems="center"
              spacing={0.5} mt={1.25}>
              <IconButton size="small"
                onClick={() => handleVote(true)}
                sx={{ color: comment.userVote === 1 ? "primary.main" : "text.secondary", p: 0.4 }}>
                <ThumbUpRounded sx={{ fontSize: 15 }} />
              </IconButton>
              <Typography variant="caption" fontWeight={600}
                color={comment.userVote !== 0 ? "primary.main" : "text.secondary"}>
                {comment.upvoteCount}
              </Typography>
              <IconButton size="small"
                onClick={() => handleVote(false)}
                sx={{ color: comment.userVote === -1 ? "error.main" : "text.secondary", p: 0.4 }}>
                <ThumbDownRounded sx={{ fontSize: 15 }} />
              </IconButton>

              <Button size="small"
                startIcon={<ReplyRounded sx={{ fontSize: 14 }} />}
                onClick={() => setReplying((r) => !r)}
                sx={{ fontSize: "0.72rem", ml: 0.5, borderRadius: 2 }}>
                Reply
              </Button>

              <Tooltip title="Report">
                <IconButton size="small"
                  onClick={() => setReportOpen(true)}
                  sx={{ color: "text.secondary", p: 0.4 }}>
                  <FlagRounded sx={{ fontSize: 15 }} />
                </IconButton>
              </Tooltip>

              {canModerate && (
                <Tooltip title="Edit">
                  <IconButton size="small"
                    onClick={() => {
                      setEditText(comment.content);
                      setEditing((e) => !e);
                    }}
                    sx={{ color: "text.secondary", p: 0.4 }}>
                    <EditRounded sx={{ fontSize: 15 }} />
                  </IconButton>
                </Tooltip>
              )}

              {canModerate && (
                <Tooltip title="Delete">
                  <IconButton size="small"
                    onClick={handleDelete}
                    sx={{ color: "error.main", p: 0.4, ml: "auto" }}>
                    <DeleteRounded sx={{ fontSize: 15 }} />
                  </IconButton>
                </Tooltip>
              )}
            </Stack>
          </Box>
        </Stack>

        {/* Reply input */}
        {replying && (
          <Box sx={{ pl: 5.5 }}>
            <Stack direction="row" spacing={1} alignItems="flex-end">
              <TextField
                size="small" fullWidth multiline maxRows={3}
                placeholder="Write a reply..."
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
                sx={{ "& .MuiOutlinedInput-root": { borderRadius: 2.5 } }}
              />
              <Button variant="contained" size="small"
                onClick={handleReply} disabled={submitting}
                sx={{
                  borderRadius: 2, whiteSpace: "nowrap",
                  background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                }}>
                {submitting ? "..." : "Reply"}
              </Button>
            </Stack>
          </Box>
        )}

        {/* Nested replies */}
        {!collapsed && comment.replies?.map((reply) => (
          <CommentItem
            key={reply.id}
            comment={reply}
            postId={postId}
            onNotify={onNotify}
            depth={depth + 1}
          />
        ))}

        <ReportDialog
          open={reportOpen}
          onClose={() => setReportOpen(false)}
          target={{ type: "comment", id: comment.id }}
          onReported={onNotify}
        />
      </Stack>
    </Box>
  );
};

export default CommentItem;
