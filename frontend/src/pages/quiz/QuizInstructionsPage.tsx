import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Stack, Typography,
} from "@mui/material";
import {
  AccessTimeRounded, MenuBookRounded, QuizRounded, RepeatRounded, TrendingUpRounded,
} from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import {
  ModuleQuizResponseDto, QuizResponseDto, AttemptStartResponseDto,
} from "../../types/quiz.types";
import { ROUTES } from "../../constants/routes";

const QuizInstructionsPage = () => {
  const { moduleId } = useParams<{ moduleId: string }>();
  const navigate = useNavigate();
  const mid = Number(moduleId);

  const [moduleQuiz, setModuleQuiz] = useState<ModuleQuizResponseDto | null>(null);
  const [quiz, setQuiz] = useState<QuizResponseDto | null>(null);
  const [starting, setStarting] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!mid) return;
    (async () => {
      setLoading(true);
      setError("");
      try {
        const mq = await quizService.getModuleQuiz(mid);
        if (!mq) { setError("No quiz assigned to this module."); return; }
        setModuleQuiz(mq);
        const q = await quizService.getById(mq.quizId);
        setQuiz(q);
      } catch { setError("Failed to load quiz information."); }
      finally { setLoading(false); }
    })();
  }, [mid]);

  const handleStart = async () => {
    if (!quiz) return;
    setStarting(true);
    setError("");
    try {
      const attempt = await quizService.startAttempt(quiz.id, mid);
      navigate(`/quiz/attempt/${attempt.attemptId}`);
    } catch (e: any) {
      setError(e?.response?.data?.message || "Failed to start quiz.");
    } finally { setStarting(false); }
  };

  if (loading) return <Box sx={{ textAlign: "center", py: 12 }}><CircularProgress /></Box>;

  if (error && !quiz) return (
    <Box sx={{ maxWidth: 600, mx: "auto", py: 8, textAlign: "center" }}>
      <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>
      <Button variant="outlined" onClick={() => navigate(-1)}>Go Back</Button>
    </Box>
  );

  return (
    <Box sx={{ maxWidth: 700, mx: "auto", py: 6, px: 2 }}>
      <Box sx={{ textAlign: "center", mb: 4 }}>
        <QuizRounded sx={{ fontSize: 64, color: "primary.main", mb: 2 }} />
        <Typography variant="h4" fontWeight={700}>{quiz?.title}</Typography>
        {moduleQuiz && (
          <Typography variant="body1" color="text.secondary">{moduleQuiz.quizTitle}</Typography>
        )}
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError("")}>{error}</Alert>}

      <Card sx={{ borderRadius: 4, mb: 3 }}>
        <CardContent sx={{ p: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={2}>Instructions</Typography>
          <Stack spacing={2}>
            <Stack direction="row" spacing={1.5} alignItems="center">
              <MenuBookRounded color="primary" />
              <Typography variant="body2">Read each question carefully before answering.</Typography>
            </Stack>
            <Stack direction="row" spacing={1.5} alignItems="center">
              <TrendingUpRounded color="primary" />
              <Typography variant="body2">
                Passing score: <strong>{quiz?.passingPercentage}%</strong>
              </Typography>
            </Stack>
            {quiz?.timeLimitMinutes && (
              <Stack direction="row" spacing={1.5} alignItems="center">
                <AccessTimeRounded color="warning" />
                <Typography variant="body2">
                  Time limit: <strong>{quiz.timeLimitMinutes} minutes</strong>
                </Typography>
              </Stack>
            )}
            <Stack direction="row" spacing={1.5} alignItems="center">
              <RepeatRounded color="primary" />
              <Typography variant="body2">
                Maximum attempts: <strong>{quiz?.maximumAttempts}</strong>
              </Typography>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Card sx={{ borderRadius: 4, mb: 4 }}>
        <CardContent sx={{ p: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={2}>Quiz Details</Typography>
          <Stack spacing={1}>
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Questions</Typography>
              <Typography fontWeight={600}>{quiz?.questionCount}</Typography>
            </Stack>
            {quiz?.difficultyFilter !== null && (
              <Stack direction="row" justifyContent="space-between">
                <Typography color="text.secondary">Difficulty</Typography>
                <Chip label={quiz?.difficultyFilter !== null ? ["Easy", "Medium", "Hard"][quiz?.difficultyFilter ?? 0] : "Mixed"} size="small" />
              </Stack>
            )}
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Selection</Typography>
              <Typography fontWeight={600}>{quiz?.selectionMode === 0 ? "Random" : "Sequential"}</Typography>
            </Stack>
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Passing Percentage</Typography>
              <Typography fontWeight={600}>{quiz?.passingPercentage}%</Typography>
            </Stack>
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Max Attempts</Typography>
              <Typography fontWeight={600}>{quiz?.maximumAttempts}</Typography>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Box sx={{ textAlign: "center" }}>
        <Button variant="contained" size="large" onClick={handleStart} disabled={starting}
          sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2, px: 6, py: 1.5 }}>
          {starting ? "Starting..." : "Start Quiz"}
        </Button>
        <Box mt={2}>
          <Button variant="text" onClick={() => navigate(ROUTES.LEARNING_PATHS)}>
            Back to Learning Paths
          </Button>
        </Box>
      </Box>
    </Box>
  );
};

export default QuizInstructionsPage;
