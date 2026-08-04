import { useLocation, useNavigate } from "react-router-dom";
import { Stack, Chip } from "@mui/material";
import { ROUTES } from "../../../constants/routes";
import { useAuth } from "../../../hooks/useAuth";

const CommunityNav = () => {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const { isAdmin } = useAuth();

  const onGroups      = pathname === ROUTES.COMMUNITY
    || pathname.startsWith(ROUTES.COMMUNITY_GROUPS);
  const onAllPosts    = pathname === ROUTES.COMMUNITY_POSTS;
  const onMyPosts     = pathname === ROUTES.COMMUNITY_MY_POSTS;
  const onModeration  = pathname === ROUTES.COMMUNITY_MODERATION;

  const items = [
    { label: "Groups",     active: onGroups,     onClick: () => navigate(ROUTES.COMMUNITY) },
    { label: "All Posts",  active: onAllPosts,   onClick: () => navigate(ROUTES.COMMUNITY_POSTS) },
    { label: "My Posts",   active: onMyPosts,    onClick: () => navigate(ROUTES.COMMUNITY_MY_POSTS) },
  ];

  if (isAdmin) {
    items.push({ label: "Moderation", active: onModeration, onClick: () => navigate(ROUTES.COMMUNITY_MODERATION) });
  }

  return (
    <Stack direction="row" spacing={1} flexWrap="wrap" gap={1} mb={3}>
      {items.map((item) => (
        <Chip
          key={item.label}
          label={item.label}
          clickable
          onClick={item.onClick}
          color={item.active ? "primary" : "default"}
          sx={{
            fontWeight: 600,
            fontSize: "0.8rem",
            ...(item.active && {
              background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
              color: "#fff",
            }),
          }}
        />
      ))}
    </Stack>
  );
};

export default CommunityNav;
