import {
  Box, Card, CardActionArea, CardContent,
  Chip, IconButton, Stack, Tooltip, Typography,
} from "@mui/material";
import {
  ThumbUpRounded, ThumbDownRounded,
  CommentRounded, VisibilityRounded,
  PushPinRounded, LockRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { PostSummary } from "../../../types/community.types";
import { AppDispatch } from "../../../redux/store";
import { updatePostVote } from "../../../redux/slices/communitySlice";
import { communityService } from "../../../services/communityService";
import { dateUtils } from "../../../utils/dateUtils";

const CATEGORY_COLORS: Record<string, string> = {
  General:       "#6C63FF",
  Questions:     "#3B82F6",
  Resources:     "#22C55E",
  Projects:      "#F59E0B",
  Announcements: "#EF4444",
};

interface PostCardProps {
  post: PostSummary;
}

const PostCard = ({ post }: PostCardProps) => {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();

  const handleVote = async (
    e: React.MouseEvent,
    isUpvote: boolean
  ) => {
    e.stopPropagation();
    try {
      const newCount = await communityService.votePost(post.id, isUpvote);
      const newVote =
        post.userVote === (isUpvote ? 1 : -1) ? 0 : (isUpvote ? 1 : -1);
      dispatch(updatePostVote({
        postId: post.id,
        upvoteCount: newCount,
        userVote: newVote,
      }));
    } catch { /* silent */ }
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
              <Typography variant="caption" color="text.secondary">
                {dateUtils.timeAgo(post.createdAt)}
              </Typography>
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

            {/* Footer */}
            <Stack direction="row" alignItems="center"
              justifyContent="space-between" pt={0.5}>
              <Typography variant="caption" color="text.secondary" fontWeight={500}>
                {post.authorName}
              </Typography>

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
              </Stack>
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
};

export default PostCard;
