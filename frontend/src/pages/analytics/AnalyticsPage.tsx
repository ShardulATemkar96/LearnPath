import { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Grid, Skeleton, Stack, Typography,
} from "@mui/material";
import {
  CheckCircleRounded, LocalFireDepartmentRounded,
  RouteRounded, WorkspacePremiumRounded,
  ClassRounded, BarChartRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import { fetchAnalytics } from "../../redux/slices/analyticsSlice";
import {
  selectAnalyticsData,
  selectAnalyticsLoading,
  selectAnalyticsError,
} from "../../redux/selectors/analyticsSelectors";
import WeeklyBarChart    from "../../components/analytics/WeeklyBarChart/WeeklyBarChart";
import PathProgressChart from "../../components/analytics/PathProgressChart/PathProgressChart";
import ModuleTypeChart   from "../../components/analytics/ModuleTypeChart/ModuleTypeChart";
import MiniStatCard      from "../../components/analytics/MiniStatCard/MiniStatCard";

const MINI_STATS = (data: NonNullable<ReturnType<typeof selectAnalyticsData>>) => [
  {
    label: "Modules Completed",
    value: data.totalModulesCompleted,
    icon: <CheckCircleRounded />,
    color: "#22C55E",
  },
  {
    label: "Paths Enrolled",
    value: data.totalPathsEnrolled,
    icon: <RouteRounded />,
    color: "#6C63FF",
  },
  {
    label: "Certificates",
    value: data.totalCertificates,
    icon: <WorkspacePremiumRounded />,
    color: "#F59E0B",
  },
  {
    label: "Active Classrooms",
    value: data.activeClassrooms,
    icon: <ClassRounded />,
    color: "#3B82F6",
  },
  {
    label: "Day Streak",
    value: `${data.currentStreak} 🔥`,
    icon: <LocalFireDepartmentRounded />,
    color: "#EF4444",
  },
  {
    label: "Avg Completion",
    value: `${data.averageCompletionRate}%`,
    icon: <BarChartRounded />,
    color: "#FF6584",
  },
];

const AnalyticsPage = () => {
  const dispatch = useDispatch<AppDispatch>();
  const data     = useSelector(selectAnalyticsData);
  const loading  = useSelector(selectAnalyticsLoading);
  const error    = useSelector(selectAnalyticsError);

  useEffect(() => { dispatch(fetchAnalytics()); }, [dispatch]);

  return (
    <Box>
      <Stack spacing={0.5} mb={4}>
        <Typography variant="h4" fontWeight={700}>Analytics</Typography>
        <Typography variant="body2" color="text.secondary">
          Track your learning progress and activity.
        </Typography>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      <Grid container spacing={2.5} mb={4}>
        {loading || !data
          ? Array.from({ length: 6 }).map((_, i) => (
              <Grid item xs={12} sm={6} md={4} lg={2} key={i}>
                <Skeleton variant="rounded" height={88} sx={{ borderRadius: 3 }} />
              </Grid>
            ))
          : MINI_STATS(data).map((s) => (
              <Grid item xs={12} sm={6} md={4} lg={2} key={s.label}>
                <MiniStatCard {...s} />
              </Grid>
            ))}
      </Grid>

      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} md={7}>
          {loading || !data
            ? <Skeleton variant="rounded" height={320} sx={{ borderRadius: 4 }} />
            : <WeeklyBarChart data={data.weeklyActivity} />}
        </Grid>
        <Grid item xs={12} md={5}>
          {loading || !data
            ? <Skeleton variant="rounded" height={320} sx={{ borderRadius: 4 }} />
            : <ModuleTypeChart data={data.moduleTypeBreakdown} />}
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        <Grid item xs={12}>
          {loading || !data
            ? <Skeleton variant="rounded" height={300} sx={{ borderRadius: 4 }} />
            : <PathProgressChart data={data.pathCompletions} />}
        </Grid>
      </Grid>
    </Box>
  );
};

export default AnalyticsPage;
