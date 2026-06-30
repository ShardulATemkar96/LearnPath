import { useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import { fetchPathById, clearSelectedPath } from "../../redux/slices/pathSlice";
import { selectSelectedPath, selectPathDetailLoading } from "../../redux/selectors/pathSelectors";
import { progressService } from "../../services/progressService";
import { Box, Button, Typography, Chip, Alert, Skeleton, Stack, Divider } from "@mui/material";
import { ArrowBackRounded, CheckCircleRounded } from "@mui/icons-material";
import { ROUTES } from "../../constants/routes";
import { AppDispatch } from "@/redux/store";

const LessonPage = () => {
  const { pathId, moduleId } = useParams<{ pathId: string; moduleId: string }>();
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const path = useSelector(selectSelectedPath);
  const loading = useSelector(selectPathDetailLoading);

  useEffect(() => {
    if (pathId) dispatch(fetchPathById(Number(pathId)));
    return () => { dispatch(clearSelectedPath()); };
  }, [pathId, dispatch]);

  const module = path?.modules?.find((m) => m.id === Number(moduleId));

  if (loading) return <Skeleton height={400} sx={{ borderRadius: 4 }} />;
  if (!module) return <Alert severity="error">Module not found.</Alert>;

  return (
    <Box>
      <Button startIcon={<ArrowBackRounded />} onClick={() => navigate(ROUTES.LEARNING_PATH_DETAIL.replace(":id", pathId!))} sx={{ mb: 3, color: "text.secondary" }}>
        Back to Path
      </Button>

      <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center">
            <Chip label={module.contentType} size="small" color="primary" />
            {module.isCompleted && <Chip label="Completed" size="small" color="success" />}
          </Stack>
          <Typography variant="h4" fontWeight={700}>{module.title}</Typography>
          <Typography variant="body1" color="text.secondary">{module.description}</Typography>
        </Stack>
      </Box>

      {module.contentBody && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3, "& img": { maxWidth: "100%" } }}
          dangerouslySetInnerHTML={{ __html: module.contentBody }} />
      )}

      {module.contentUrl && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
          <Typography variant="body2" color="text.secondary" mb={1}>Resource link:</Typography>
          <a href={module.contentUrl} target="_blank" rel="noopener noreferrer">{module.contentUrl}</a>
        </Box>
      )}

      {module.isUnlocked && !module.isCompleted && (
        <Button variant="contained" size="large" fullWidth sx={{ borderRadius: 3, py: 1.5 }}
          startIcon={<CheckCircleRounded />}
          onClick={async () => {
            try {
              await progressService.markComplete(module.id);
              if (pathId) dispatch(fetchPathById(Number(pathId)));
            } catch (e: any) { console.error(e); }
          }}>
          Mark as Complete
        </Button>
      )}
    </Box>
  );
};

export default LessonPage;