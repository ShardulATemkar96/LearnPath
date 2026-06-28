import { useState } from "react";
import {
  Avatar, Box, Button, IconButton,
  Stack, TextField, Tooltip, Typography,
} from "@mui/material";
import {
  ThumbUpRounded, ReplyRounded,
  ThumbDownRounded, DeleteRounded,
} from "@mui/icons-material";
import { Comment } from "../../../types/community.types";
import { communityService } from "../../../services/communityService";
import { dateUtils } from "../../../utils/dateUtils";
import { useAuth } from "../../../hooks/useAuth";

interface CommentItemProps {
  comment: Comment;
  postId: number;
  onReplyAdded: (comment: Comment, parentId: number) => void;
  onDeleted: (commentId: number) => void;
  depth?: number;
}

const CommentItem = ({
  comment, postId, onReplyAdded, onDeleted, depth = 0,
}: CommentItemProps) => {
  const { user } = useAuth();
  const [replying,  setReplying]  = useState(false);
  const [replyText, setReplyText] = useState("");
  const [votes, setVotes]         = useState(comment.upvoteCount);
  const [userVote, setUserVote]   = useState(comment.userVote);
  const [submitting, setSubmitting] = useState(false);

  const handleVote = async (isUpvote: boolean) => {
    try {
      const newCount = await communityService.voteComment(comment.id, isUpvote);
      setVotes(newCount);
      setUserVote(userVote === (isUpvote ? 1 : -1) ? 0 : (isUpvote ? 1 : -1));
    } catch { /* silent */ }
  };

  const handleReply = async () => {
    if (!replyText.trim()) return;
    setSubmitting(true);
    try {
      const newComment = await communityService.addComment(postId, {
        content: replyText,
        parentCommentId: comment.id,
      });
      onReplyAdded(newComment, comment.id);
      setReplyText("");
      setReplying(false);
    } finally { setSubmitting(false); }
  };

  const handleDelete = async () => {
    if (!window.confirm("Delete this comment?")) return;
    try {
      await communityService.deleteComment(comment.id);
      onDeleted(comment.id);
    } catch { /* silent */ }
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
              <Typography variant="body2" fontWeight={700}>
                {comment.authorName}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {dateUtils.timeAgo(comment.createdAt)}
              </Typography>
            </Stack>

            <Typography variant="body2" color="text.primary" lineHeight={1.6}>
              {comment.content}
            </Typography>

            {/* Actions */}
            <Stack direction="row" alignItems="center"
              spacing={0.5} mt={1.25}>
              <IconButton size="small"
                onClick={() => handleVote(true)}
                sx={{ color: userVote === 1 ? "primary.main" : "text.secondary", p: 0.4 }}>
                <ThumbUpRounded sx={{ fontSize: 15 }} />
              </IconButton>
              <Typography variant="caption" fontWeight={600}
                color={userVote !== 0 ? "primary.main" : "text.secondary"}>
                {votes}
              </Typography>
              <IconButton size="small"
                onClick={() => handleVote(false)}
                sx={{ color: userVote === -1 ? "error.main" : "text.secondary", p: 0.4 }}>
                <ThumbDownRounded sx={{ fontSize: 15 }} />
              </IconButton>

              {depth === 0 && (
                <Button size="small"
                  startIcon={<ReplyRounded sx={{ fontSize: 14 }} />}
                  onClick={() => setReplying((r) => !r)}
                  sx={{ fontSize: "0.72rem", ml: 0.5, borderRadius: 2 }}>
                  Reply
                </Button>
              )}

              {user?.userId === comment.authorId && (
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
        {comment.replies?.map((reply) => (
          <CommentItem
            key={reply.id}
            comment={reply}
            postId={postId}
            onReplyAdded={onReplyAdded}
            onDeleted={onDeleted}
            depth={depth + 1}
          />
        ))}
      </Stack>
    </Box>
  );
};

export default CommentItem;
