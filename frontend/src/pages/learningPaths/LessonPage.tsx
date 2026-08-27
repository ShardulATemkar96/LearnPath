import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { pathService } from "../../services/pathService";
import { progressService } from "../../services/progressService";
import { Module } from "../../types/path.types";
import { Box, Button, Typography, Chip, Alert, Skeleton, Stack, Divider, Grid, Paper, Tooltip } from "@mui/material";
import { ArrowBackRounded, CheckCircleRounded, LockRounded, ChevronLeftRounded, ChevronRightRounded } from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import { ModuleQuizResponseDto } from "../../types/quiz.types";
import { ROUTES } from "../../constants/routes";
import { selectUserRoles } from "../../redux/selectors/authSelectors";

const LessonPage = () => {
  const { pathId, moduleId } = useParams<{ pathId: string; moduleId: string }>();
  const navigate = useNavigate();
  const roles = useSelector(selectUserRoles);
  const canManuallyComplete = roles.includes("Instructor") || roles.includes("Admin");
  const [module, setModule] = useState<Module | null>(null);
  const [moduleQuiz, setModuleQuiz] = useState<ModuleQuizResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [siblingUnlock, setSiblingUnlock] = useState<{ prev: boolean; next: boolean }>({ prev: true, next: true });

  useEffect(() => {
    if (!pathId || !moduleId) return;
    setLoading(true);
    setError("");
    Promise.all([
      pathService.getModuleContent(Number(pathId), Number(moduleId)),
      quizService.getModuleQuiz(Number(moduleId)).catch(() => null),
      pathService.getById(Number(pathId)).catch(() => null),
    ])
      .then(([mod, mq, pathDetail]) => {
        setModule(mod);
        setModuleQuiz(mq);
        if (pathDetail) {
          const mods = pathDetail.modules;
          const currentIdx = mods.findIndex(m => m.id === mod.id);
          setSiblingUnlock({
            prev: currentIdx > 0 ? mods[currentIdx - 1].isUnlocked : false,
            next: currentIdx >= 0 && currentIdx < mods.length - 1 ? mods[currentIdx + 1].isUnlocked : false,
          });
        }
      })
      .catch((e: any) => {
        const msg = e?.response?.data?.message || e?.message || "Failed to load module.";
        if (e?.response?.status === 403) {
          setError("Please complete the previous module to unlock this module.");
        } else {
          setError("Module not found.");
        }
      })
      .finally(() => setLoading(false));
  }, [pathId, moduleId]);

  const handleMarkComplete = async () => {
    if (!module) return;
    try {
      await progressService.markComplete(module.id);
      const updated = await pathService.getModuleContent(Number(pathId), Number(moduleId));
      setModule(updated);
    } catch (e: any) { console.error(e); }
  };

  if (loading) return <Skeleton height={400} sx={{ borderRadius: 4 }} />;

  if (error) return (
    <Box sx={{ textAlign: "center", py: 8 }}>
      <LockRounded sx={{ fontSize: 64, color: "text.disabled", mb: 2 }} />
      <Alert severity="warning" sx={{ mb: 2, maxWidth: 500, mx: "auto" }}>{error}</Alert>
      <Button startIcon={<ArrowBackRounded />} onClick={() => navigate(ROUTES.LEARNING_PATH_DETAIL.replace(":id", pathId!))}>
        Back to Path
      </Button>
    </Box>
  );

  if (!module) return null;

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

      {module.notesHtml && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3, "& img": { maxWidth: "100%" } }}
          dangerouslySetInnerHTML={{ __html: module.notesHtml }} />
      )}

      {module.pdfUrl && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
          <Typography variant="body2" color="text.secondary" mb={1}>PDF Notes:</Typography>
          <Button variant="outlined" component="a" href={module.pdfUrl} target="_blank" rel="noopener noreferrer">
            Open PDF
          </Button>
        </Box>
      )}

      {module.objectives.length > 0 && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={2}>Learning Objectives</Typography>
          <ul style={{ margin: 0, paddingLeft: 20 }}>
            {module.objectives.map((o, i) => (
              <li key={o.id || i}><Typography variant="body2" color="text.secondary">{o.objectiveText}</Typography></li>
            ))}
          </ul>
        </Box>
      )}

      {module.resources.length > 0 && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={2}>Resources</Typography>
          <Stack spacing={1.5}>
            {module.resources.map((r, i) => (
              <Box key={r.id || i} sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                <Chip label={r.type} size="small" variant="outlined" />
                <Typography variant="body2" fontWeight={500}>{r.title}</Typography>
                <Button size="small" variant="text" component="a" href={r.url} target="_blank" rel="noopener noreferrer">
                  Open
                </Button>
              </Box>
            ))}
          </Stack>
        </Box>
      )}

      {moduleQuiz && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={1}>Quiz</Typography>
          <Typography variant="body2" color="text.secondary" mb={1}>
            {moduleQuiz.quizTitle}
          </Typography>
          <Button variant="contained" sx={{ borderRadius: 2 }}
            onClick={() => navigate(`/quiz/instructions/${moduleId}`)}>
            Start Quiz
          </Button>
        </Box>
      )}

      {!module.isCompleted && canManuallyComplete && (
        <Button variant="contained" size="large" fullWidth sx={{ borderRadius: 3, py: 1.5 }}
          startIcon={<CheckCircleRounded />}
          onClick={handleMarkComplete}>
          Mark as Complete
        </Button>
      )}

      {!module.isCompleted && !canManuallyComplete && moduleQuiz && (
        <Alert severity="info" sx={{ borderRadius: 2 }}>
          Complete and pass the module quiz to unlock completion.
        </Alert>
      )}

      <Stack direction="row" justifyContent="space-between" mt={4}>
        <Tooltip title={!module.previousModuleId ? "This is the first module" : !siblingUnlock.prev ? "Previous module is locked" : ""}>
          <span>
            <Button
              startIcon={<ChevronLeftRounded />}
              variant="outlined"
              disabled={!module.previousModuleId || !siblingUnlock.prev}
              onClick={() => navigate(`/paths/${pathId}/modules/${module.previousModuleId}`)}
              sx={{ borderRadius: 2 }}>
              Previous
            </Button>
          </span>
        </Tooltip>
        <Tooltip title={!module.nextModuleId ? "This is the last module" : !siblingUnlock.next ? "Complete this module's quiz to unlock the next module" : ""}>
          <span>
            <Button
              endIcon={<ChevronRightRounded />}
              variant="outlined"
              disabled={!module.nextModuleId || !siblingUnlock.next}
              onClick={() => navigate(`/paths/${pathId}/modules/${module.nextModuleId}`)}
              sx={{ borderRadius: 2 }}>
              Next
            </Button>
          </span>
        </Tooltip>
      </Stack>
    </Box>
  );
};

export default LessonPage;