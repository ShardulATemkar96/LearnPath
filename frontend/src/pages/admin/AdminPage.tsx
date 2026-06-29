import { useEffect, useState } from "react";
import {
  Alert, Box, Card, CardContent,
  CircularProgress, Grid, Stack, Typography,
} from "@mui/material";
import { adminService } from "../../services/adminService";
import { AdminStats } from "../../types/admin.types";

const AdminPage = () => {
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
      <Grid container spacing={3}>
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
    </Box>
  );
};

export default AdminPage;
