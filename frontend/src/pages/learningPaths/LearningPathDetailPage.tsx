import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { progressService } from "../../services/progressService";

import {
  Alert, Box, Button, Chip, Divider,
  Grid, Skeleton, Stack, Typography,Tabs, Tab,
} from "@mui/material";
import {
  ArrowBackRounded, CheckCircleRounded,
  LockRounded, PlayArrowRounded, LayersRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import PathGraph from "../../components/learningPath/PathGraph/PathGraph";  
import { fetchPathById, clearSelectedPath } from "../../redux/slices/pathSlice";
import {
  selectSelectedPath,
  selectPathDetailLoading,
  selectPathError,
} from "../../redux/selectors/pathSelectors";
import { Module } from "../../types/path.types";
import { ROUTES } from "../../constants/routes";

const ModuleCard = ({ module, dispatch, pathId ,navigate }: { module: Module; dispatch: AppDispatch; pathId: number ; navigate:(path:string) =>void;}) => (
  <Box 
    onClick={() => {
      if(module.isUnlocked){
        navigate(`/paths/${pathId}/modules/${module.id}`);
      }
    }}
    sx={{
      p: 2.5,
      borderRadius: 3,
      border: "1.5px solid",
      borderColor: module.isCompleted
        ? "success.main"
        : module.isUnlocked
        ? "primary.light"
        : "divider",
      bgcolor: module.isCompleted
        ? "rgba(34,197,94,0.04)"
        : module.isUnlocked
        ? "background.paper"
        : "rgba(0,0,0,0.02)",
      opacity: module.isUnlocked ? 1 : 0.65,
      transition: "border-color 0.2s",
    }}
  >
    <Stack direction="row" alignItems="center" spacing={2}>
      <Box
        sx={{
          width: 40, height: 40, borderRadius: 2, flexShrink: 0,
          display: "flex", alignItems: "center", justifyContent: "center",
          bgcolor: module.isCompleted
            ? "rgba(34,197,94,0.12)"
            : module.isUnlocked
            ? "rgba(108,99,255,0.1)"
            : "rgba(0,0,0,0.05)",
        }}
      >
        {module.isCompleted
          ? <CheckCircleRounded sx={{ color: "success.main" }} />
          : module.isUnlocked
          ? <PlayArrowRounded sx={{ color: "primary.main" }} />
          : <LockRounded sx={{ color: "text.disabled", fontSize: 18 }} />}
      </Box>

      <Box sx={{ flexGrow: 1, minWidth: 0 }}>
        <Typography variant="body1" fontWeight={600} noWrap>
          {module.title}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {module.contentType}
        </Typography>
      </Box>

      <Chip
        label={
          module.isCompleted ? "Done"
          : module.isUnlocked ? "Start"
          : "Locked"
        }
        size="small"
        color={module.isCompleted ? "success" : module.isUnlocked ? "primary" : "default"}
        sx={{ fontWeight: 600, fontSize: "0.7rem" }}
      />
    </Stack>

    {module.description && (
      <Typography variant="body2" color="text.secondary" mt={1.5} ml={7}>
        {module.description}
      </Typography>
    )}
  {module.isUnlocked && !module.isCompleted && (
  <Box mt={1.5} ml={7}>
    <Button
      size="small"
      variant="contained"
      onClick={async () => {
        try {
          await progressService.markComplete(module.id);
          // Refresh path after completion
          dispatch(fetchPathById(pathId));
        } catch (e: any) {
          console.error(e);
        }
      }}
      sx={{
        fontSize: "0.75rem", borderRadius: 2,
        background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
      }}
    >
      Mark Complete
    </Button>
  </Box>
)}
  </Box>
  
);

const LearningPathDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();

  const [detailTab, setDetailTab] = useState(0);

  const path    = useSelector(selectSelectedPath);
  const loading = useSelector(selectPathDetailLoading);
  const error   = useSelector(selectPathError);

  useEffect(() => {
    if (id) dispatch(fetchPathById(Number(id)));
    return () => { dispatch(clearSelectedPath()); };
  }, [id, dispatch]);

  if (loading) {
    return (
      <Box>
        <Skeleton height={40} width={200} sx={{ mb: 2 }} />
        <Skeleton variant="rounded" height={180} sx={{ borderRadius: 4, mb: 3 }} />
        <Grid container spacing={2}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Grid item xs={12} key={i}>
              <Skeleton variant="rounded" height={80} sx={{ borderRadius: 3 }} />
            </Grid>
          ))}
        </Grid>
      </Box>
    );
  }

  if (error) return <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>;
  if (!path) return null;

  const completedCount = path.modules.filter((m) => m.isCompleted).length;
  const progressPct = path.totalModules > 0
    ? Math.round((completedCount / path.totalModules) * 100)
    : 0;

  return (
    <Box>
      {/* Back */}
      <Button
        startIcon={<ArrowBackRounded />}
        onClick={() => navigate(ROUTES.LEARNING_PATHS)}
        sx={{ mb: 3, color: "text.secondary" }}
      >
        All Paths
      </Button>

      {/* Header */}
      <Box
        sx={{
          p: 4, borderRadius: 4, mb: 4,
          background: "linear-gradient(135deg, #6C63FF18 0%, #9D97FF10 100%)",
          border: "1px solid rgba(108,99,255,0.12)",
        }}
      >
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} flexWrap="wrap">
            {path.isPublic && (
              <Chip label="Public" size="small" color="primary" sx={{ fontWeight: 600 }} />
            )}
            {path.isPublished
              ? <Chip label="Published" size="small" color="success" sx={{ fontWeight: 600 }} />
              : <Chip label="Draft" size="small" sx={{ fontWeight: 600 }} />}
          </Stack>

          <Typography variant="h4" fontWeight={700}>{path.title}</Typography>
          <Typography variant="body1" color="text.secondary">{path.description}</Typography>

          <Divider />

          <Stack direction="row" spacing={4} flexWrap="wrap">
            <Stack direction="row" alignItems="center" spacing={1}>
              <LayersRounded sx={{ color: "primary.main", fontSize: 20 }} />
              <Typography variant="body2" fontWeight={500}>
                {path.totalModules} Modules
              </Typography>
            </Stack>
            <Stack direction="row" alignItems="center" spacing={1}>
              <CheckCircleRounded sx={{ color: "success.main", fontSize: 20 }} />
              <Typography variant="body2" fontWeight={500}>
                {completedCount} Completed ({progressPct}%)
              </Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              by {path.createdByName}
            </Typography>
          </Stack>
        </Stack>
      </Box>

     
      {/* Detail Tabs */}
<Tabs
  value={detailTab}
  onChange={(_, v) => setDetailTab(v)}
  sx={{
    mb: 3,
    "& .MuiTab-root": { fontWeight: 600, textTransform: "none" },
    "& .MuiTabs-indicator": {
      background: "linear-gradient(90deg, #6C63FF, #9D97FF)",
      height: 3, borderRadius: 2,
    },
  }}
>
  <Tab label="Modules" />
  <Tab label="Graph View" />
</Tabs>

{detailTab === 0 && (
  path.modules.length === 0 ? (
    <Alert severity="info" sx={{ borderRadius: 2 }}>
      No modules have been added to this path yet.
    </Alert>
  ) : (
    <Stack spacing={1.5}>
      {path.modules.map((module) => (
        <ModuleCard key={module.id} module={module} dispatch={dispatch} pathId={Number(id)} navigate={navigate} />
      ))}
    </Stack>
  )
)}

{detailTab === 1 && (
  <Box sx={{
    p: 3, borderRadius: 4,
    border: "1.5px solid", borderColor: "divider",
    bgcolor: "background.paper",
  }}>
    <PathGraph
      modules={path.modules}
      dependencies={path.dependencies}
    />
  </Box>
)}
      
    </Box>
  );
};

export default LearningPathDetailPage;