import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Stack, Typography,
} from "@mui/material";
import { CheckCircleRounded, CancelRounded, ArrowBackRounded } from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import { ReviewResponseDto } from "../../types/quiz.types";

const QuizReviewPage = () => {
  const { attemptId } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();
  const aid = Number(attemptId);

  const [review, setReview] = useState<ReviewResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!aid) return;
    (async () => {
      setLoading(true);
      setError("");
      try { setReview(await quizService.getReview(aid)); }
      catch { setError("Failed to load review."); }
      finally { setLoading(false); }
    })();
  }, [aid]);

  if (loading) return <Box sx={{ textAlign: "center", py: 12 }}><CircularProgress /></Box>;
  if (error || !review) return (
    <Box sx={{ textAlign: "center", py: 8 }}><Typography color="error">{error || "Not found"}</Typography></Box>
  );

  return (
    <Box sx={{ maxWidth: 800, mx: "auto", py: 4, px: 2 }}>
      <Stack direction="row" alignItems="center" spacing={1} mb={3}>
        <Button onClick={() => navigate(`/quiz/result/${aid}`)}><ArrowBackRounded /></Button>
        <Typography variant="h5" fontWeight={700}>Answer Review</Typography>
        <Chip label={`Score: ${review.percentage}%`}
          color={review.passed ? "success" : "error"} size="small" />
      </Stack>

      <Stack spacing={2}>
        {review.questions.map((q, i) => {
          const correct = q.selectedOptionId === q.correctOptionId;
          return (
            <Card key={q.questionId} sx={{ borderRadius: 3 }}>
              <CardContent sx={{ p: 2.5 }}>
                <Stack direction="row" spacing={1.5} alignItems="flex-start" mb={1}>
                  {correct
                    ? <CheckCircleRounded color="success" fontSize="small" sx={{ mt: 0.3 }} />
                    : <CancelRounded color="error" fontSize="small" sx={{ mt: 0.3 }} />}
                  <Box>
                    <Typography fontWeight={600} mb={1}>{q.questionText}</Typography>
                    <Stack spacing={0.5}>
                      {q.options.map((opt) => {
                        let color: "success" | "error" | "text.secondary" | undefined;
                        if (opt.optionId === q.correctOptionId) color = "success";
                        else if (opt.optionId === q.selectedOptionId && !correct) color = "error";
                        return (
                          <Typography key={opt.optionId} variant="body2"
                            fontWeight={color ? 600 : 400}
                            color={color || "text.secondary"}>
                            {opt.optionText}
                            {opt.optionId === q.correctOptionId && " ✓"}
                            {opt.optionId === q.selectedOptionId && opt.optionId !== q.correctOptionId && " (Your answer)"}
                          </Typography>
                        );
                      })}
                    </Stack>
                    {!correct && (
                      <Typography variant="caption" color="error" sx={{ mt: 1, display: "block" }}>
                        Correct answer: {q.options.find((o) => o.optionId === q.correctOptionId)?.optionText}
                      </Typography>
                    )}
                    {q.explanation && (
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                        Explanation: {q.explanation}
                      </Typography>
                    )}
                  </Box>
                </Stack>
              </CardContent>
            </Card>
          );
        })}
      </Stack>

      <Box sx={{ textAlign: "center", mt: 4 }}>
        <Button variant="contained" onClick={() => navigate(`/quiz/result/${aid}`)}
          sx={{ borderRadius: 2 }}>
          Back to Results
        </Button>
      </Box>
    </Box>
  );
};

export default QuizReviewPage;
