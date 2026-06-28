import { useEffect, useState } from "react";
import {
  AppBar, Avatar, Badge, Box, Divider, IconButton,
  InputAdornment, List, ListItem, ListItemText,
  Popover, Stack, TextField, Toolbar, Typography, Button,
} from "@mui/material";
import {
  NotificationsRounded, SearchRounded, DoneAllRounded,
} from "@mui/icons-material";
import { useDispatch, useSelector } from "react-redux";
import { AppDispatch } from "../../../redux/store";
import {
  fetchNotifications,
  markAllReadThunk,
  markReadThunk,
} from "../../../redux/slices/notificationSlice";
import {
  selectNotifications,
  selectUnreadCount,
} from "../../../redux/selectors/notificationSelectors";
import { useAuth } from "../../../hooks/useAuth";

interface TopbarProps { pageTitle?: string; }

const Topbar = ({ pageTitle }: TopbarProps) => {
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useAuth();

  const notifications = useSelector(selectNotifications);
  const unreadCount   = useSelector(selectUnreadCount);

  const [anchor, setAnchor] = useState<HTMLButtonElement | null>(null);

  useEffect(() => { dispatch(fetchNotifications()); }, [dispatch]);

  const handleOpen  = (e: React.MouseEvent<HTMLButtonElement>) =>
    setAnchor(e.currentTarget);
  const handleClose = () => setAnchor(null);

  const TYPE_COLOR: Record<string, string> = {
    completion:  "#22C55E",
    achievement: "#F59E0B",
    join:        "#6C63FF",
    default:     "#6B7280",
  };

  return (
    <AppBar position="sticky" elevation={0} sx={{
      bgcolor: "background.paper",
      borderBottom: "1px solid", borderColor: "divider",
      color: "text.primary",
    }}>
      <Toolbar sx={{ gap: 2, minHeight: "68px !important" }}>
        {pageTitle && (
          <Typography variant="h6" fontWeight={600} sx={{ flexShrink: 0 }}>
            {pageTitle}
          </Typography>
        )}

        <Box sx={{ flexGrow: 1, maxWidth: 420 }}>
          <TextField
            size="small" fullWidth
            placeholder="Search paths, modules, classrooms..."
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRounded sx={{ color: "text.secondary", fontSize: 20 }} />
                </InputAdornment>
              ),
            }}
            sx={{
              "& .MuiOutlinedInput-root": {
                bgcolor: "background.default",
                borderRadius: 3, fontSize: "0.875rem",
              },
            }}
          />
        </Box>

        <Box sx={{ flexGrow: 1 }} />

        <Stack direction="row" alignItems="center" spacing={1}>
          <IconButton size="small" onClick={handleOpen}>
            <Badge badgeContent={unreadCount} color="error">
              <NotificationsRounded sx={{ color: "text.secondary" }} />
            </Badge>
          </IconButton>

          <Avatar
            sx={{
              width: 34, height: 34, fontSize: "0.8rem",
              fontWeight: 700,
              background: "linear-gradient(135deg, #6C63FF, #FF6584)",
              cursor: "pointer",
            }}
          >
            {user?.firstName?.[0]}{user?.lastName?.[0]}
          </Avatar>
        </Stack>
      </Toolbar>

      <Popover
        open={Boolean(anchor)}
        anchorEl={anchor}
        onClose={handleClose}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
        PaperProps={{
          sx: {
            width: 360, borderRadius: 3, mt: 1,
            boxShadow: "0 8px 32px rgba(0,0,0,0.12)",
          },
        }}
      >
        <Stack
          direction="row" alignItems="center"
          justifyContent="space-between"
          sx={{ px: 2.5, py: 2 }}
        >
          <Typography variant="h6" fontWeight={700} fontSize="1rem">
            Notifications
            {unreadCount > 0 && (
              <Typography
                component="span"
                variant="caption"
                sx={{
                  ml: 1, px: 1, py: 0.25, borderRadius: 10,
                  bgcolor: "error.main", color: "#fff", fontWeight: 700,
                }}
              >
                {unreadCount}
              </Typography>
            )}
          </Typography>
          {unreadCount > 0 && (
            <Button
              size="small"
              startIcon={<DoneAllRounded sx={{ fontSize: 16 }} />}
              onClick={() => dispatch(markAllReadThunk())}
              sx={{ fontSize: "0.75rem", borderRadius: 2 }}
            >
              Mark all read
            </Button>
          )}
        </Stack>

        <Divider />

        <List sx={{ p: 0, maxHeight: 360, overflowY: "auto" }}>
          {notifications.length === 0 ? (
            <Box sx={{ py: 5, textAlign: "center" }}>
              <Typography variant="body2" color="text.secondary">
                No notifications yet.
              </Typography>
            </Box>
          ) : (
            notifications.slice(0, 10).map((n) => (
              <ListItem
                key={n.id}
                onClick={() => !n.isRead && dispatch(markReadThunk(n.id))}
                sx={{
                  cursor: n.isRead ? "default" : "pointer",
                  bgcolor: n.isRead ? "transparent" : "rgba(108,99,255,0.04)",
                  borderLeft: `3px solid`,
                  borderColor: n.isRead
                    ? "transparent"
                    : (TYPE_COLOR[n.type] ?? TYPE_COLOR.default),
                  "&:hover": { bgcolor: "action.hover" },
                  transition: "background 0.15s",
                }}
                divider
              >
                <ListItemText
                  primary={
                    <Typography variant="body2" fontWeight={n.isRead ? 400 : 600}>
                      {n.title}
                    </Typography>
                  }
                  secondary={
                    <Stack spacing={0.25}>
                      <Typography variant="caption" color="text.secondary">
                        {n.message}
                      </Typography>
                      <Typography variant="caption" color="text.disabled">
                        {new Date(n.createdAt).toLocaleDateString("en-US", {
                          month: "short", day: "numeric",
                          hour: "2-digit", minute: "2-digit",
                        })}
                      </Typography>
                    </Stack>
                  }
                />
              </ListItem>
            ))
          )}
        </List>
      </Popover>
    </AppBar>
  );
};

export default Topbar;
