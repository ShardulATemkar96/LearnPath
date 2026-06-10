import {
  Box, Drawer, List, ListItemButton, ListItemIcon,
  ListItemText, Stack, Typography, Avatar, Divider, Tooltip,
} from "@mui/material";
import {
  DashboardRounded, RouteRounded, ClassRounded,
  BarChartRounded, GroupsRounded, WorkspacePremiumRounded,
  PersonRounded, SettingsRounded, AdminPanelSettingsRounded,
  LogoutRounded,
} from "@mui/icons-material";
import { useLocation, useNavigate } from "react-router-dom";
import { ROUTES } from "../../../constants/routes";
import { useAuth } from "../../../hooks/useAuth";

const SIDEBAR_WIDTH = 260;

interface NavItem {
  label: string;
  icon: React.ReactNode;
  path: string;
  adminOnly?: boolean;
}

const NAV_ITEMS: NavItem[] = [
  { label: "Dashboard",       icon: <DashboardRounded />,           path: ROUTES.DASHBOARD },
  { label: "Learning Paths",  icon: <RouteRounded />,               path: ROUTES.LEARNING_PATHS },
  { label: "Classrooms",      icon: <ClassRounded />,               path: ROUTES.CLASSROOM },
  { label: "Analytics",       icon: <BarChartRounded />,            path: ROUTES.ANALYTICS },
  { label: "Community",       icon: <GroupsRounded />,              path: ROUTES.COMMUNITY },
  { label: "Certificates",    icon: <WorkspacePremiumRounded />,    path: ROUTES.CERTIFICATES },
  { label: "Profile",         icon: <PersonRounded />,              path: ROUTES.PROFILE },
  { label: "Settings",        icon: <SettingsRounded />,            path: ROUTES.SETTINGS },
  { label: "Admin",           icon: <AdminPanelSettingsRounded />,  path: ROUTES.ADMIN, adminOnly: true },
];

const Sidebar = () => {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const { user, isAdmin, logout } = useAuth();

  const visibleItems = NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin);

  const handleLogout = async () => {
    await logout();
    navigate(ROUTES.LOGIN, { replace: true });
  };

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: SIDEBAR_WIDTH,
        flexShrink: 0,
        "& .MuiDrawer-paper": {
          width: SIDEBAR_WIDTH,
          boxSizing: "border-box",
          border: "none",
          background: "linear-gradient(180deg, #1A1D2E 0%, #252840 100%)",
          display: "flex",
          flexDirection: "column",
        },
      }}
    >
      {/* Logo */}
      <Stack
        direction="row"
        alignItems="center"
        spacing={1.5}
        sx={{ px: 3, py: 3 }}
      >
        <Box
          sx={{
            width: 36,
            height: 36,
            borderRadius: 2,
            background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <RouteRounded sx={{ color: "#fff", fontSize: 20 }} />
        </Box>
        <Typography
          variant="h6"
          fontWeight={700}
          sx={{
            background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
            WebkitBackgroundClip: "text",
            WebkitTextFillColor: "transparent",
          }}
        >
          LearnPath
        </Typography>
      </Stack>

      <Divider sx={{ borderColor: "rgba(255,255,255,0.06)", mx: 2 }} />

      {/* Nav Items */}
      <List sx={{ px: 1.5, pt: 2, flexGrow: 1 }}>
        {visibleItems.map((item) => {
          const isActive = pathname === item.path;
          return (
            <Tooltip key={item.path} title="" placement="right">
              <ListItemButton
                onClick={() => navigate(item.path)}
                sx={{
                  borderRadius: 2,
                  mb: 0.5,
                  px: 2,
                  py: 1.2,
                  color: isActive ? "#fff" : "rgba(255,255,255,0.55)",
                  background: isActive
                    ? "linear-gradient(135deg, rgba(108,99,255,0.35), rgba(108,99,255,0.15))"
                    : "transparent",
                  borderLeft: isActive ? "3px solid #6C63FF" : "3px solid transparent",
                  "&:hover": {
                    background: "rgba(108,99,255,0.15)",
                    color: "#fff",
                  },
                  transition: "all 0.2s ease",
                }}
              >
                <ListItemIcon
                  sx={{
                    minWidth: 36,
                    color: isActive ? "#6C63FF" : "rgba(255,255,255,0.45)",
                  }}
                >
                  {item.icon}
                </ListItemIcon>
                <ListItemText
                  primary={item.label}
                  primaryTypographyProps={{
                    fontSize: "0.875rem",
                    fontWeight: isActive ? 600 : 400,
                  }}
                />
              </ListItemButton>
            </Tooltip>
          );
        })}
      </List>

      <Divider sx={{ borderColor: "rgba(255,255,255,0.06)", mx: 2 }} />

      {/* User Footer */}
      <Stack
        direction="row"
        alignItems="center"
        spacing={1.5}
        sx={{ px: 2.5, py: 2.5 }}
      >
        <Avatar
          sx={{
            width: 36,
            height: 36,
            background: "linear-gradient(135deg, #6C63FF, #FF6584)",
            fontSize: "0.875rem",
            fontWeight: 700,
          }}
        >
          {user?.firstName?.[0]}{user?.lastName?.[0]}
        </Avatar>
        <Box sx={{ flexGrow: 1, minWidth: 0 }}>
          <Typography
            variant="body2"
            fontWeight={600}
            noWrap
            sx={{ color: "#fff" }}
          >
            {user?.firstName} {user?.lastName}
          </Typography>
          <Typography
            variant="caption"
            noWrap
            sx={{ color: "rgba(255,255,255,0.4)" }}
          >
            {user?.roles?.[0]}
          </Typography>
        </Box>
        <Tooltip title="Logout">
          <ListItemIcon
            onClick={handleLogout}
            sx={{
              minWidth: "unset",
              color: "rgba(255,255,255,0.4)",
              cursor: "pointer",
              "&:hover": { color: "#FF6584" },
              transition: "color 0.2s",
            }}
          >
            <LogoutRounded fontSize="small" />
          </ListItemIcon>
        </Tooltip>
      </Stack>
    </Drawer>
  );
};

export default Sidebar;
