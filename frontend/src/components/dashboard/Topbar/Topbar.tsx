import {
  AppBar, Avatar, Badge, Box, IconButton,
  InputAdornment, Stack, TextField, Toolbar, Typography,
} from "@mui/material";
import {
  NotificationsRounded, SearchRounded,
} from "@mui/icons-material";
import { useAuth } from "../../../hooks/useAuth";

interface TopbarProps {
  pageTitle?: string;
}

const Topbar = ({ pageTitle }: TopbarProps) => {
  const { user } = useAuth();

  return (
    <AppBar
      position="sticky"
      elevation={0}
      sx={{
        bgcolor: "background.paper",
        borderBottom: "1px solid",
        borderColor: "divider",
        color: "text.primary",
      }}
    >
      <Toolbar sx={{ gap: 2, minHeight: "68px !important" }}>
        {/* Page Title */}
        {pageTitle && (
          <Typography variant="h6" fontWeight={600} sx={{ flexShrink: 0 }}>
            {pageTitle}
          </Typography>
        )}

        {/* Search */}
        <Box sx={{ flexGrow: 1, maxWidth: 420 }}>
          <TextField
            size="small"
            placeholder="Search paths, modules, classrooms..."
            fullWidth
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
                borderRadius: 3,
                fontSize: "0.875rem",
              },
            }}
          />
        </Box>

        <Box sx={{ flexGrow: 1 }} />

        {/* Actions */}
        <Stack direction="row" alignItems="center" spacing={1}>
          <IconButton size="small">
            <Badge badgeContent={3} color="error">
              <NotificationsRounded sx={{ color: "text.secondary" }} />
            </Badge>
          </IconButton>

          <Avatar
            sx={{
              width: 34,
              height: 34,
              fontSize: "0.8rem",
              fontWeight: 700,
              background: "linear-gradient(135deg, #6C63FF, #FF6584)",
              cursor: "pointer",
            }}
          >
            {user?.firstName?.[0]}{user?.lastName?.[0]}
          </Avatar>
        </Stack>
      </Toolbar>
    </AppBar>
  );
};

export default Topbar;
