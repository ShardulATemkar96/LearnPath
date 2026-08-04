import {
  Box, Card, CardActionArea, CardContent,
  Chip, Stack, Typography,
} from "@mui/material";
import {
  GroupsRounded, PersonRounded, ArticleRounded,
  CalendarMonthRounded, LockRounded, PublicRounded,
} from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { Group } from "../../../types/community.types";
import { dateUtils } from "../../../utils/dateUtils";

interface GroupCardProps {
  group: Group;
}

const GroupCard = ({ group }: GroupCardProps) => {
  const navigate = useNavigate();

  return (
    <Card
      sx={{
        borderRadius: 4,
        boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
        transition: "transform 0.22s ease, box-shadow 0.22s ease",
        "&:hover": {
          transform: "translateY(-3px)",
          boxShadow: "0 10px 36px rgba(108,99,255,0.14)",
        },
        height: "100%",
      }}
    >
      <CardActionArea
        onClick={() => navigate(`/community/groups/${group.id}`)}
        sx={{ height: "100%", display: "flex", flexDirection: "column", alignItems: "stretch" }}
      >
        <Box
          sx={{
            height: 6,
            background: group.isPublic
              ? "linear-gradient(90deg, #6C63FF, #9D97FF)"
              : "linear-gradient(90deg, #FF6584, #FF92A8)",
          }}
        />

        <CardContent sx={{ p: 3, flexGrow: 1 }}>
          <Stack spacing={2}>
            <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
              <Box
                sx={{
                  width: 44, height: 44, borderRadius: 2.5, flexShrink: 0,
                  display: "flex", alignItems: "center", justifyContent: "center",
                  background: "rgba(108,99,255,0.1)",
                }}
              >
                <GroupsRounded sx={{ color: "primary.main" }} />
              </Box>
              <Chip
                icon={group.isPublic
                  ? <PublicRounded sx={{ fontSize: 14 }} />
                  : <LockRounded sx={{ fontSize: 14 }} />}
                label={group.isPublic ? "Public" : "Private"}
                size="small"
                color={group.isPublic ? "primary" : "default"}
                sx={{ fontWeight: 600, fontSize: "0.7rem" }}
              />
            </Stack>

            <Box>
              <Typography variant="h6" fontWeight={700} gutterBottom
                sx={{
                  display: "-webkit-box",
                  WebkitLineClamp: 2,
                  WebkitBoxOrient: "vertical",
                  overflow: "hidden",
                }}
              >
                {group.name}
              </Typography>
              <Typography variant="body2" color="text.secondary"
                sx={{
                  display: "-webkit-box",
                  WebkitLineClamp: 2,
                  WebkitBoxOrient: "vertical",
                  overflow: "hidden",
                }}
              >
                {group.description}
              </Typography>
            </Box>

            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <GroupsRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary">
                  {group.memberCount} members
                </Typography>
              </Stack>
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <ArticleRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary">
                  {group.postCount} posts
                </Typography>
              </Stack>
              <Stack direction="row" alignItems="center" spacing={0.5}>
                <PersonRounded sx={{ fontSize: 16, color: "text.secondary" }} />
                <Typography variant="caption" color="text.secondary" noWrap
                  sx={{ maxWidth: 120 }}>
                  {group.ownerName}
                </Typography>
              </Stack>
            </Stack>

            <Stack direction="row" alignItems="center" spacing={0.5}>
              <CalendarMonthRounded sx={{ fontSize: 15, color: "text.secondary" }} />
              <Typography variant="caption" color="text.secondary">
                Created {dateUtils.format(group.createdAt)}
              </Typography>
            </Stack>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
};

export default GroupCard;
