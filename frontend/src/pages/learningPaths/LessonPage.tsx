import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { pathService } from "../../services/pathService";
import { progressService } from "../../services/progressService";
import { Module } from "../../types/path.types";
import { Box, Button, Typography, Chip, Alert, Skeleton, Stack, Divider, Grid, Paper } from "@mui/material";
import { ArrowBackRounded, CheckCircleRounded, LockRounded, ChevronLeftRounded, ChevronRightRounded } from "@mui/icons-material";
import { ROUTES } from "../../constants/routes";
import QuizPage from "../../components/quiz/QuizPage";

const LessonPage = () => {
  const { pathId, moduleId } = useParams<{ pathId: string; moduleId: string }>();
  const navigate = useNavigate();
  const [module, setModule] = useState<Module | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [quizActive, setQuizActive] = useState(false);

  useEffect(() => {
    if (!pathId || !moduleId) return;
    setLoading(true);
    setError("");
    pathService.getModuleContent(Number(pathId), Number(moduleId))
      .then(setModule)
      .catch((e: any) => {
        const msg = e?.response?.data?.message || e?.message || "Failed to load module.";
        if (e?.response?.status === 403) {
          setError(msg);
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

      {module.quizEnabled && !quizActive && (
        <Box sx={{ p: 4, borderRadius: 4, border: "1px solid", borderColor: "divider", mb: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={1}>Quiz</Typography>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Passing score: {module.quizPassingScore}% | Questions: {module.quizQuestionCount}
            {module.quizTimeLimitMinutes && ` | Time limit: ${module.quizTimeLimitMinutes} min`}
          </Typography>
          <Button variant="contained" sx={{ borderRadius: 2 }} onClick={() => setQuizActive(true)}>
            Start Quiz
          </Button>
        </Box>
      )}

      {quizActive && (
        <QuizPage
          moduleId={module.id}
          onClose={() => setQuizActive(false)}
        />
      )}

      {!module.isCompleted && (
        <Button variant="contained" size="large" fullWidth sx={{ borderRadius: 3, py: 1.5 }}
          startIcon={<CheckCircleRounded />}
          onClick={handleMarkComplete}>
          Mark as Complete
        </Button>
      )}

      <Stack direction="row" justifyContent="space-between" mt={4}>
        <Button
          startIcon={<ChevronLeftRounded />}
          variant="outlined"
          disabled={!module.previousModuleId}
          onClick={() => navigate(`/paths/${pathId}/modules/${module.previousModuleId}`)}
          sx={{ borderRadius: 2 }}>
          Previous
        </Button>
        <Button
          endIcon={<ChevronRightRounded />}
          variant="outlined"
          disabled={!module.nextModuleId}
          onClick={() => navigate(`/paths/${pathId}/modules/${module.nextModuleId}`)}
          sx={{ borderRadius: 2 }}>
          Next
        </Button>
      </Stack>
    </Box>
  );
};

export default LessonPage;