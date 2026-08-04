import { useState } from "react";
import {
  Box, Card, CardActionArea, CardContent,
  Chip, IconButton, Snackbar, Stack, Tooltip, Typography,
} from "@mui/material";
import {
  ThumbUpRounded, ThumbDownRounded,
  CommentRounded, VisibilityRounded,
  PushPinRounded, LockRounded, FlagRounded, GroupsRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { PostSummary } from "../../../types/community.types";
import { AppDispatch } from "../../../redux/store";
import { votePostThunk } from "../../../redux/slices/communitySlice";
import { dateUtils } from "../../../utils/dateUtils";
import ReportDialog from "../ReportDialog/ReportDialog";

const CATEGORY_COLORS: Record<string, string> = {
  General:       "#6C63FF",
  Questions:     "#3B82F6",
  Resources:     "#22C55E",
  Projects:      "#F59E0B",
  Announcements: "#EF4444",
};

interface PostCardProps {
  post: PostSummary;
  canPin?: boolean;
  onPinToggle?: (post: PostSummary) => void;
  onTagClick?: (tag: string) => void;
}

const PostCard = ({ post, canPin, onPinToggle, onTagClick }: PostCardProps) => {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const [toast, setToast] = useState("");
  const [reportOpen, setReportOpen] = useState(false);

  const handleVote = async (
    e: React.MouseEvent,
    isUpvote: boolean
  ) => {
    e.stopPropagation();
    const result = await dispatch(votePostThunk({ postId: post.id, isUpvote }));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setToast("Vote updated.");
    }
  };

  return (
    <Card
      sx={{
        borderRadius: 3,
        boxShadow: "0 2px 12px rgba(0,0,0,0.06)",
        transition: "transform 0.18s, box-shadow 0.18s",
        "&:hover": {
          transform: "translateY(-2px)",
          boxShadow: "0 6px 24px rgba(108,99,255,0.12)",
        },
        border: post.isPinned
          ? "1.5px solid rgba(108,99,255,0.3)"
          : "1.5px solid transparent",
      }}
    >
      <CardActionArea
        onClick={() => navigate(`/community/${post.id}`)}
        sx={{ display: "block" }}
      >
        <CardContent sx={{ p: 2.5 }}>
          <Stack spacing={1.5}>
            {/* Header row */}
            <Stack direction="row" alignItems="center"
              justifyContent="space-between" flexWrap="wrap" gap={1}>
              <Stack direction="row" alignItems="center" spacing={1}>
                <Chip
                  label={post.category}
                  size="small"
                  sx={{
                    fontWeight: 600,
                    fontSize: "0.7rem",
                    bgcolor: `${CATEGORY_COLORS[post.category] ?? "#6C63FF"}18`,
                    color: CATEGORY_COLORS[post.category] ?? "#6C63FF",
                  }}
                />
                {post.groupName && (
                  <Chip
                    label={post.groupName}
                    size="small"
                    variant="outlined"
                    icon={<GroupsRounded sx={{ fontSize: 13 }} />}
                    onClick={(e) => {
                      if (!post.groupId) return;
                      e.stopPropagation();
                      navigate(`/community/groups/${post.groupId}`);
                    }}
                    sx={{
                      fontWeight: 600,
                      fontSize: "0.7rem",
                      borderColor: "rgba(108,99,255,0.4)",
                      color: "primary.main",
                      cursor: "pointer",
                    }}
                  />
                )}
                {post.isPinned && (
                  <Tooltip title="Pinned">
                    <PushPinRounded
                      sx={{ fontSize: 16, color: "primary.main" }}
                    />
                  </Tooltip>
                )}
                {post.isLocked && (
                  <Tooltip title="Locked">
                    <LockRounded
                      sx={{ fontSize: 16, color: "text.disabled" }}
                    />
                  </Tooltip>
                )}
              </Stack>
              <Stack direction="row" alignItems="center" spacing={0.5}>
                {canPin && onPinToggle && (
                  <Tooltip title={post.isPinned ? "Unpin" : "Pin"}>
                    <IconButton
                      size="small"
                      onClick={(e) => {
                        e.stopPropagation();
                        onPinToggle(post);
                      }}
                      sx={{
                        color: post.isPinned
                          ? "primary.main" : "text.secondary",
                        p: 0.5,
                      }}
                    >
                      <PushPinRounded sx={{ fontSize: 16 }} />
                    </IconButton>
                  </Tooltip>
                )}
                <Typography variant="caption" color="text.secondary">
                  {post.editedAt
                    ? <>Edited · {dateUtils.timeAgo(post.editedAt)}</>
                    : dateUtils.timeAgo(post.createdAt)}
                </Typography>
              </Stack>
            </Stack>

            {/* Title */}
            <Typography variant="h6" fontWeight={700}
              sx={{
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
                overflow: "hidden",
                lineHeight: 1.4,
              }}
            >
              {post.title}
            </Typography>

            {/* Preview */}
            <Typography variant="body2" color="text.secondary"
              sx={{
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
                overflow: "hidden",
              }}
            >
              {post.contentPreview}
            </Typography>

            {/* Tags */}
            {post.tags && (
              <Stack direction="row" spacing={0.5} flexWrap="wrap" gap={0.5}>
                {post.tags.split(",").map((tag) => tag.trim())
                  .filter(Boolean).map((tag) => (
                    <Chip
                      key={tag}
                      label={tag}
                      size="small"
                      variant="outlined"
                      onClick={(e) => {
                        if (!onTagClick) return;
                        e.stopPropagation();
                        onTagClick(tag);
                      }}
                      sx={{
                        fontSize: "0.7rem", fontWeight: 600,
                        cursor: onTagClick ? "pointer" : "default",
                      }}
                    />
                  ))}
              </Stack>
            )}

            {/* Footer */}
            <Stack direction="row" alignItems="center"
              justifyContent="space-between" pt={0.5}>
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <Typography variant="caption" color="text.secondary" fontWeight={500}>
                  {post.authorIsDeleted ? "Deleted User" : post.authorName}
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

              <Stack direction="row" alignItems="center" spacing={0.5}>
                {/* Upvote */}
                <IconButton
                  size="small"
                  onClick={(e) => handleVote(e, true)}
                  sx={{
                    color: post.userVote === 1
                      ? "primary.main" : "text.secondary",
                    p: 0.5,
                  }}
                >
                  <ThumbUpRounded sx={{ fontSize: 16 }} />
                </IconButton>
                <Typography variant="caption" fontWeight={600}
                  color={post.userVote !== 0 ? "primary.main" : "text.secondary"}>
                  {post.upvoteCount}
                </Typography>

                {/* Downvote */}
                <IconButton
                  size="small"
                  onClick={(e) => handleVote(e, false)}
                  sx={{
                    color: post.userVote === -1
                      ? "error.main" : "text.secondary",
                    p: 0.5,
                  }}
                >
                  <ThumbDownRounded sx={{ fontSize: 16 }} />
                </IconButton>

                {/* Comments */}
                <Stack direction="row" alignItems="center"
                  spacing={0.4} ml={1}>
                  <CommentRounded
                    sx={{ fontSize: 15, color: "text.secondary" }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {post.commentCount}
                  </Typography>
                </Stack>

                {/* Views */}
                <Stack direction="row" alignItems="center"
                  spacing={0.4} ml={0.5}>
                  <VisibilityRounded
                    sx={{ fontSize: 15, color: "text.secondary" }}
                  />
                  <Typography variant="caption" color="text.secondary">
                    {post.viewCount}
                  </Typography>
                </Stack>

                {/* Report */}
                <Tooltip title="Report">
                  <IconButton
                    size="small"
                    onClick={(e) => {
                      e.stopPropagation();
                      setReportOpen(true);
                    }}
                    sx={{ p: 0.5, ml: 1 }}
                  >
                    <FlagRounded sx={{ fontSize: 15, color: "text.secondary" }} />
                  </IconButton>
                </Tooltip>
              </Stack>
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>

      <ReportDialog
        open={reportOpen}
        onClose={() => setReportOpen(false)}
        target={{ type: "post", id: post.id }}
        onReported={(msg) => setToast(msg)}
      />

      <Snackbar
        open={!!toast}
        autoHideDuration={2500}
        onClose={() => setToast("")}
        message={toast}
      />
    </Card>
  );
};

export default PostCard;
