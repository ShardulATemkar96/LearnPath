import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Grid, Skeleton,
  Stack, Tab, Tabs, Typography,
} from "@mui/material";
import { AddRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import { fetchPublicPaths, fetchMyPaths } from "../../redux/slices/pathSlice";
import {
  selectPublicPaths, selectMyPaths,
  selectPathLoading, selectPathError,
} from "../../redux/selectors/pathSelectors";
import PathCard from "../../components/learningPath/PathCard/PathCard";
import CreatePathModal from "../../components/learningPath/CreatePathModal/CreatePathModal";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import { LayersRounded } from "@mui/icons-material";

const LearningPathsPage = () => {
  const dispatch = useDispatch<AppDispatch>();
  const [tab, setTab] = useState(0);
  const [modalOpen, setModalOpen] = useState(false);

  const publicPaths = useSelector(selectPublicPaths);
  const myPaths     = useSelector(selectMyPaths);
  const loading     = useSelector(selectPathLoading);
  const error       = useSelector(selectPathError);

  useEffect(() => {
    dispatch(fetchPublicPaths());
    dispatch(fetchMyPaths());
  }, [dispatch]);

  const displayedPaths = tab === 0 ? publicPaths : myPaths;

  return (
    <Box>
      {/* Header */}
      <Stack direction="row" alignItems="center" justifyContent="space-between" mb={4}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Learning Paths</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Explore curated graph-based learning journeys.
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          onClick={() => setModalOpen(true)}
          sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}
        >
          New Path
        </Button>
      </Stack>

      {/* Tabs */}
      <Tabs
        value={tab}
        onChange={(_, v) => setTab(v)}
        sx={{
          mb: 3,
          "& .MuiTab-root": { fontWeight: 600, textTransform: "none" },
          "& .MuiTabs-indicator": {
            background: "linear-gradient(90deg, #6C63FF, #9D97FF)",
            height: 3,
            borderRadius: 2,
          },
        }}
      >
        <Tab label={`Public Paths (${publicPaths.length})`} />
        <Tab label={`My Paths (${myPaths.length})`} />
      </Tabs>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      {/* Grid */}
      {loading ? (
        <Grid container spacing={3}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Grid item xs={12} sm={6} lg={4} key={i}>
              <Skeleton variant="rounded" height={300} sx={{ borderRadius: 4 }} />
            </Grid>
          ))}
        </Grid>
      ) : displayedPaths.length === 0 ? (
        <EmptyState
          title={tab === 0 ? "No public paths yet." : "You haven't created any paths."}
          description={tab === 1 ? "Click 'New Path' to get started." : undefined}
          icon={<LayersRounded sx={{ fontSize: 48 }} />}
        />
      ) : (
        <Grid container spacing={3}>
          {displayedPaths.map((path) => (
            <Grid item xs={12} sm={6} lg={4} key={path.id}>
              <PathCard path={path} />
            </Grid>
          ))}
        </Grid>
      )}

      <CreatePathModal open={modalOpen} onClose={() => setModalOpen(false)} />
    </Box>
  );
};

export default LearningPathsPage;