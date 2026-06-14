tsximport { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Grid, Skeleton, Stack, Typography,
} from "@mui/material";
import {
  RouteRounded, CheckCircleRounded,
  ClassRounded, WorkspacePremiumRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import { fetchDashboardData } from "../../redux/slices/dashboardSlice";
import {
  selectDashboardStats,
  selectPathProgress,
  selectRecentActivity,
  selectDashboardLoading,
  selectDashboardError,
} from "../../redux/selectors/dashboardSelectors";
import { useAuth } from "../../hooks/useAuth";
import StatCard from "../../components/dashboard/StatCard/StatCard";
import ActivityFeed from "../../components/dashboard/ActivityFeed/ActivityFeed";
import ProgressOverview from "../../components/dashboard/ProgressOverview/ProgressOverview";

const STAT_CONFIG = [
  {
    key: "enrolledPaths" as const,
    title: "Enrolled Paths",
    icon: <RouteRounded />,
    gradient: "linear-gradient(135deg, #6C63FF, #9D97FF)",
    subtitle: "Active enrollments",
    trend: { value: 12, label: "this month" },
  },
  {
    key: "completedModules" as const,
    title: "Modules Completed",
    icon: <CheckCircleRounded />,
    gradient: "linear-gradient(135deg, #22C55E, #4ADE80)",
    subtitle: "Across all paths",
    trend: { value: 8, label: "this week" },
  },
  {
    key: "activeClassrooms" as const,
    title: "Active Classrooms",
    icon: <ClassRounded />,
    gradient: "linear-gradient(135deg, #3B82F6, #60A5FA)",
    subtitle: "Joined classrooms",
  },
  {
    key: "certificates" as const,
    title: "Certificates",
    icon: <WorkspacePremiumRounded />,
    gradient: "linear-gradient(135deg, #F59E0B, #FCD34D)",
    subtitle: "Earned so far",
  },
];

const DashboardPage = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { user } = useAuth();

  const stats         = useSelector(selectDashboardStats);
  const pathProgress  = useSelector(selectPathProgress);
  const recentActivity = useSelector(selectRecentActivity);
  const loading       = useSelector(selectDashboardLoading);
  const error         = useSelector(selectDashboardError);

  useEffect(() => {
    dispatch(fetchDashboardData());
  }, [dispatch]);

  return (
    <Box>
      {/* Greeting */}
      <Stack spacing={0.5} mb={4}>
        <Typography variant="h4" fontWeight={700} color="text.primary">
          Good to see you, {user?.firstName} 👋
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Here's your learning overview for today.
        </Typography>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      {/* Stat Cards */}
      <Grid container spacing={3} mb={4}>
        {STAT_CONFIG.map((cfg) => (
          <Grid item xs={12} sm={6} lg={3} key={cfg.key}>
            {loading || !stats ? (
              <Skeleton variant="rounded" height={140} sx={{ borderRadius: 4 }} />
            ) : (
              <StatCard
                title={cfg.title}
                value={stats[cfg.key] ?? 0}
                subtitle={cfg.subtitle}
                icon={cfg.icon}
                gradient={cfg.gradient}
                trend={cfg.trend}
              />
            )}
          </Grid>
        ))}
      </Grid>

      {/* Bottom Section */}
      <Grid container spacing={3}>
        <Grid item xs={12} md={7}>
          {loading ? (
            <Skeleton variant="rounded" height={360} sx={{ borderRadius: 4 }} />
          ) : (
            <ProgressOverview paths={pathProgress} />
          )}
        </Grid>
        <Grid item xs={12} md={5}>
          {loading ? (
            <Skeleton variant="rounded" height={360} sx={{ borderRadius: 4 }} />
          ) : (
            <ActivityFeed items={recentActivity} />
          )}
        </Grid>
      </Grid>
    </Box>
  );
};

export default DashboardPage;
