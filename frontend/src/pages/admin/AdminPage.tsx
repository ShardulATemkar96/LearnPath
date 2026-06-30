import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Box, Card, CardContent, CardActionArea,
  CircularProgress, Grid, Stack, Typography,
} from "@mui/material";
import {
  RouteRounded, PeopleRounded, ClassRounded, WorkspacePremiumRounded,
  AdminPanelSettingsRounded,
} from "@mui/icons-material";
import { adminService } from "../../services/adminService";
import { AdminStats } from "../../types/admin.types";

const NAV_CARDS = [
  { label: "Manage Paths", desc: "Edit, delete, or manage modules", path: "/admin/paths", icon: <RouteRounded sx={{ fontSize: 40 }} /> },
  { label: "Manage Users", desc: "Change roles, activate, delete users", path: "/admin/users", icon: <PeopleRounded sx={{ fontSize: 40 }} /> },
];

const AdminPage = () => {
  const navigate = useNavigate();
  const [stats, setStats] = useState<AdminStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    (async () => {
      try {
        const data = await adminService.getStats();
        setStats(data);
      } catch { setError("Failed to load admin stats."); }
      finally { setLoading(false); }
    })();
  }, []);

  if (loading) return <Box sx={{ p: 4, textAlign: "center" }}><CircularProgress /></Box>;
  if (error) return <Box sx={{ p: 4 }}><Alert severity="error">{error}</Alert></Box>;
  if (!stats) return null;

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h4" fontWeight={700} mb={4}>Admin Panel</Typography>

      <Grid container spacing={3} mb={4}>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ borderRadius: 4 }}>
            <CardContent>
              <Typography variant="h5" fontWeight={700}>{stats.totalUsers}</Typography>
              <Typography variant="body2" color="text.secondary">Total Users</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ borderRadius: 4 }}>
            <CardContent>
              <Typography variant="h5" fontWeight={700}>{stats.totalLearningPaths}</Typography>
              <Typography variant="body2" color="text.secondary">Learning Paths</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ borderRadius: 4 }}>
            <CardContent>
              <Typography variant="h5" fontWeight={700}>{stats.totalClassrooms}</Typography>
              <Typography variant="body2" color="text.secondary">Classrooms</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <Card sx={{ borderRadius: 4 }}>
            <CardContent>
              <Typography variant="h5" fontWeight={700}>{stats.totalCertificatesIssued}</Typography>
              <Typography variant="body2" color="text.secondary">Certificates</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Typography variant="h6" fontWeight={600} mb={2}>Administration</Typography>
      <Grid container spacing={3}>
        {NAV_CARDS.map((item) => (
          <Grid item xs={12} sm={6} md={4} key={item.path}>
            <Card sx={{ borderRadius: 4 }} onClick={() => navigate(item.path)}>
              <CardActionArea sx={{ p: 2 }}>
                <Stack direction="row" alignItems="center" spacing={2}>
                  <Box sx={{ color: "primary.main" }}>{item.icon}</Box>
                  <Box>
                    <Typography variant="h6" fontWeight={600}>{item.label}</Typography>
                    <Typography variant="body2" color="text.secondary">{item.desc}</Typography>
                  </Box>
                </Stack>
              </CardActionArea>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default AdminPage;
