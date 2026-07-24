import { useEffect, useState } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Divider, Stack, Typography,
} from "@mui/material";
import {
  CancelRounded, CheckCircleRounded, EmojiEventsRounded, HomeRounded, ReplayRounded,
} from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import { AttemptReviewDto } from "../../types/quiz.types";

const QuizResultPage = () => {
  const { attemptId } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();
  const aid = Number(attemptId);
  const location = useLocation();
  const passedResult = location.state?.result as AttemptReviewDto | null;

  const [result, setResult] = useState<AttemptReviewDto | null>(passedResult);
  const [loading, setLoading] = useState(!passedResult);
  const [error, setError] = useState("");

  useEffect(() => {
    if (passedResult) return;
    (async () => {
      setLoading(true);
      setError("");
      try { setResult(await quizService.getAttemptReview(aid)); }
      catch { setError("Failed to load results."); }
      finally { setLoading(false); }
    })();
  }, [aid, passedResult]);

  if (loading) return <Box sx={{ textAlign: "center", py: 12 }}><CircularProgress /></Box>;
  if (error || !result) return (
    <Box sx={{ textAlign: "center", py: 8 }}><Typography color="error">{error || "Not found"}</Typography></Box>
  );

  const passed = result.score >= result.passingPercentage;

  return (
    <Box sx={{ maxWidth: 600, mx: "auto", py: 6, px: 2, textAlign: "center" }}>
      <Box sx={{ mb: 4 }}>
        {passed ? (
          <EmojiEventsRounded sx={{ fontSize: 80, color: "#FFD700" }} />
        ) : (
          <CancelRounded sx={{ fontSize: 80, color: "error.main" }} />
        )}
        <Typography variant="h3" fontWeight={700} mt={2}>
          {passed ? "Congratulations!" : "Not this time"}
        </Typography>
        <Typography variant="h6" color="text.secondary">
          {passed ? "You passed the quiz" : "Keep practicing, you'll get it"}
        </Typography>
      </Box>

      <Card sx={{ borderRadius: 4, mb: 3 }}>
        <CardContent sx={{ p: 3 }}>
          <Typography variant="overline" color="text.secondary">Your Score</Typography>
          <Typography variant="h2" fontWeight={800}
            color={passed ? "success.main" : "error.main"}>
            {result.score}%
          </Typography>
          <Typography variant="body2" color="text.secondary" mt={1}>
            Passing percentage: {result.passingPercentage}%
          </Typography>
        </CardContent>
      </Card>

      <Card sx={{ borderRadius: 4, mb: 4 }}>
        <CardContent sx={{ p: 3 }}>
          <Typography variant="h6" fontWeight={600} mb={2}>Summary</Typography>
          <Stack spacing={1.5}>
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Correct Answers</Typography>
              <Chip label={`${result.correctCount} / ${result.totalQuestions}`} color="success" size="small" />
            </Stack>
            <Divider />
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Total Attempts</Typography>
              <Typography fontWeight={600}>{result.attemptNumber}</Typography>
            </Stack>
            <Divider />
            <Stack direction="row" justifyContent="space-between">
              <Typography color="text.secondary">Status</Typography>
              <Chip label={passed ? "Passed" : "Failed"} color={passed ? "success" : "error"} size="small" />
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Stack direction="row" spacing={2} justifyContent="center">
        <Button variant="outlined" startIcon={<ReplayRounded />}
          onClick={() => navigate(`/quiz/instructions/${result.moduleId}`)} sx={{ borderRadius: 2 }}>
          Retry
        </Button>
        <Button variant="outlined" startIcon={<HomeRounded />}
          onClick={() => navigate("/paths")} sx={{ borderRadius: 2 }}>
          Learning Paths
        </Button>
        <Button variant="contained"
          onClick={() => navigate(`/quiz/review/${aid}`)} sx={{ borderRadius: 2 }}>
          Review Answers
        </Button>
      </Stack>
    </Box>
  );
};

export default QuizResultPage;
