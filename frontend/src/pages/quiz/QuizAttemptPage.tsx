import { useEffect, useState, useCallback, useRef } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Dialog, DialogActions,
  DialogContent, DialogTitle, IconButton, Radio, Stack, Tooltip, Typography,
} from "@mui/material";
import {
  AccessTimeRounded, ArrowBackRounded, ArrowForwardRounded,
  FlagRounded, FlagOutlined, ClearRounded, MenuBookRounded,
} from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import {
  AttemptStartResponseDto, AttemptQuestionDto,
} from "../../types/quiz.types";

const QuizAttemptPage = () => {
  const { attemptId } = useParams<{ attemptId: string }>();
  const navigate = useNavigate();
  const aid = Number(attemptId);

  const [attempt, setAttempt] = useState<AttemptStartResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [currentIndex, setCurrentIndex] = useState(0);
  const [selectedAnswers, setSelectedAnswers] = useState<Record<number, number>>({});
  const [markedQuestions, setMarkedQuestions] = useState<Set<number>>(new Set());
  const [submitDialogOpen, setSubmitDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [timeLeft, setTimeLeft] = useState<number | null>(null);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    if (!aid) return;
    (async () => {
      setLoading(true);
      setError("");
      try {
        const data = await quizService.getAttempt(aid);
        setAttempt(data);
        const saved: Record<number, number> = {};
        data.questions.forEach((q) => {
          if (q.selectedOptionId) saved[q.questionId] = q.selectedOptionId;
        });
        setSelectedAnswers(saved);
        if (data.timeLimitMinutes) setTimeLeft(data.timeLimitMinutes * 60);
      } catch { setError("Failed to load attempt."); }
      finally { setLoading(false); }
    })();
  }, [aid]);

  useEffect(() => {
    if (timeLeft === null || timeLeft <= 0) return;
    timerRef.current = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev === null || prev <= 1) {
          clearInterval(timerRef.current!);
          handleAutoSubmit();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [timeLeft]);

  const handleAutoSubmit = useCallback(async () => {
    if (submitting) return;
    setSubmitting(true);
    try {
      const result = await quizService.submitAttempt(aid);
      navigate(`/quiz/result/${aid}`, { state: { result } });
    } catch { setSubmitting(true); }
  }, [aid, submitting, navigate]);

  const handleSelect = useCallback(async (questionId: number, optionId: number) => {
    setSelectedAnswers((prev) => ({ ...prev, [questionId]: optionId }));
    try {
      await quizService.saveAnswer(aid, { questionId, optionId });
    } catch { /* silent fallback */ }
  }, [aid]);

  const handleClear = useCallback((questionId: number) => {
    setSelectedAnswers((prev) => {
      const next = { ...prev };
      delete next[questionId];
      return next;
    });
  }, []);

  const toggleMark = useCallback((questionId: number) => {
    setMarkedQuestions((prev) => {
      const next = new Set(prev);
      if (next.has(questionId)) next.delete(questionId);
      else next.add(questionId);
      return next;
    });
  }, []);

  const handleSubmit = async () => {
    setSubmitting(true);
    setSubmitDialogOpen(false);
    try {
      const result = await quizService.submitAttempt(aid);
      navigate(`/quiz/result/${aid}`, { state: { result } });
    } catch (e: any) {
      setError(e?.response?.data?.message || "Submit failed.");
      setSubmitting(false);
    }
  };

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s.toString().padStart(2, "0")}`;
  };

  if (loading) return <Box sx={{ textAlign: "center", py: 12 }}><CircularProgress /></Box>;
  if (error || !attempt) return (
    <Box sx={{ maxWidth: 500, mx: "auto", py: 8, textAlign: "center" }}>
      <Typography color="error" variant="h6" gutterBottom>{error || "Attempt not found"}</Typography>
      <Button onClick={() => navigate(-1)}>Go Back</Button>
    </Box>
  );

  const questions = attempt.questions;
  const current = questions[currentIndex];
  const answeredCount = questions.filter((q) => selectedAnswers[q.questionId] !== undefined).length;
  const currentQuestionId = current?.questionId;

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "grey.50", display: "flex", flexDirection: "column" }}>
      <Box sx={{ bgcolor: "background.paper", borderBottom: "1px solid", borderColor: "divider", px: 3, py: 1.5 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Typography variant="subtitle1" fontWeight={600}>{attempt.quizTitle}</Typography>
          <Stack direction="row" spacing={3} alignItems="center">
            {timeLeft !== null && (
              <Stack direction="row" spacing={0.5} alignItems="center">
                <AccessTimeRounded color={timeLeft < 60 ? "error" : "action"} fontSize="small" />
                <Typography variant="body2" fontWeight={700}
                  color={timeLeft < 60 ? "error" : "text.primary"}>
                  {formatTime(timeLeft)}
                </Typography>
              </Stack>
            )}
            <Typography variant="body2" color="text.secondary">
              {answeredCount}/{questions.length} answered
            </Typography>
          </Stack>
        </Stack>
      </Box>

      <Box sx={{ display: "flex", flex: 1, overflow: "hidden" }}>
        <Box sx={{ width: 240, borderRight: "1px solid", borderColor: "divider", bgcolor: "background.paper", p: 2, overflowY: "auto" }}>
          <Typography variant="subtitle2" fontWeight={600} mb={1.5}>Question Palette</Typography>
          <Stack direction="row" flexWrap="wrap" gap={0.5} mb={2}>
            {questions.map((q, i) => {
              const isAnswered = selectedAnswers[q.questionId] !== undefined;
              const isMarked = markedQuestions.has(q.questionId);
              const isCurrent = i === currentIndex;
              return (
                <Tooltip key={q.questionId}
                  title={`Q${i + 1}${isMarked ? " (Marked)" : ""}${isAnswered ? " (Answered)" : ""}`}>
                  <Box
                    onClick={() => setCurrentIndex(i)}
                    sx={{
                      position: "relative",
                      width: 36, height: 36, borderRadius: 1, display: "flex", alignItems: "center",
                      justifyContent: "center", cursor: "pointer", fontWeight: 600, fontSize: 13,
                      border: isCurrent ? "2px solid" : "1px solid",
                      borderColor: isCurrent ? "primary.main"
                        : isMarked ? "warning.main"
                        : isAnswered ? "success.main"
                        : "divider",
                      bgcolor: isAnswered ? "success.light"
                        : isMarked ? "warning.light"
                        : isCurrent ? "primary.light"
                        : "background.paper",
                      color: isCurrent ? "primary.contrastText"
                        : isAnswered ? "success.contrastText"
                        : isMarked ? "warning.contrastText"
                        : "text.primary",
                    }}>
                    {i + 1}
                    {isMarked && (
                      <FlagRounded sx={{ position: "absolute", top: -4, right: -4, fontSize: 12, color: "warning.dark" }} />
                    )}
                  </Box>
                </Tooltip>
              );
            })}
          </Stack>
          <Stack spacing={0.5}>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ fontSize: 12, color: "text.secondary" }}>
              <Box sx={{ width: 14, height: 14, borderRadius: 0.5, bgcolor: "success.light", border: "1px solid", borderColor: "success.main" }} />
              <span>Answered</span>
            </Stack>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ fontSize: 12, color: "text.secondary" }}>
              <Box sx={{ width: 14, height: 14, borderRadius: 0.5, bgcolor: "warning.light", border: "1px solid", borderColor: "warning.main" }} />
              <FlagRounded sx={{ fontSize: 11, color: "warning.dark" }} />
              <span>Marked for Review</span>
            </Stack>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ fontSize: 12, color: "text.secondary" }}>
              <Box sx={{ width: 14, height: 14, borderRadius: 0.5, bgcolor: "background.paper", border: "1px solid", borderColor: "divider" }} />
              <span>Not Answered</span>
            </Stack>
            <Stack direction="row" spacing={1} alignItems="center" sx={{ fontSize: 12, color: "text.secondary" }}>
              <Box sx={{ width: 14, height: 14, borderRadius: 0.5, bgcolor: "primary.light", border: "2px solid", borderColor: "primary.main" }} />
              <span>Current</span>
            </Stack>
          </Stack>
        </Box>

        <Box sx={{ flex: 1, p: 4, overflowY: "auto" }}>
          {current && (
            <Box key={current.questionId}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Typography variant="overline" color="text.secondary" fontWeight={600}>
                    Question {currentIndex + 1} of {questions.length}
                  </Typography>
                  {markedQuestions.has(current.questionId) && (
                    <Chip icon={<FlagRounded />} label="Marked" size="small" color="warning" />
                  )}
                </Stack>
              </Stack>

              <Typography variant="h6" fontWeight={600} mb={3}>
                {current.questionText}
              </Typography>

              <Stack spacing={1.5}>
                {current.options.map((opt) => {
                  const isSelected = selectedAnswers[current.questionId] === opt.optionId;
                  return (
                    <Card key={opt.optionId}
                      onClick={() => handleSelect(current.questionId, opt.optionId)}
                      sx={{
                        borderRadius: 3, cursor: "pointer", transition: "all 0.15s",
                        border: isSelected ? "2px solid" : "1px solid",
                        borderColor: isSelected ? "primary.main" : "divider",
                        bgcolor: isSelected ? "primary.light" : "background.paper",
                        "&:hover": { borderColor: "primary.main", bgcolor: isSelected ? "primary.light" : "action.hover" },
                      }}>
                      <CardContent sx={{ display: "flex", alignItems: "center", gap: 2, py: 2, "&:last-child": { pb: 2 } }}>
                        <Radio checked={isSelected} />
                        <Typography fontWeight={isSelected ? 600 : 400}>{opt.optionText}</Typography>
                      </CardContent>
                    </Card>
                  );
                })}
              </Stack>

              <Stack direction="row" spacing={2} mt={3}>
                <Button
                  variant={markedQuestions.has(current.questionId) ? "contained" : "outlined"}
                  color="warning"
                  size="small"
                  startIcon={markedQuestions.has(current.questionId) ? <FlagRounded /> : <FlagOutlined />}
                  onClick={() => toggleMark(current.questionId)}
                  sx={{ borderRadius: 2 }}>
                  {markedQuestions.has(current.questionId) ? "Marked for Review" : "Mark for Review"}
                </Button>
                {selectedAnswers[current.questionId] !== undefined && (
                  <Button
                    variant="outlined"
                    color="inherit"
                    size="small"
                    startIcon={<ClearRounded />}
                    onClick={() => handleClear(current.questionId)}
                    sx={{ borderRadius: 2, color: "text.secondary", borderColor: "divider" }}>
                    Clear Response
                  </Button>
                )}
              </Stack>
            </Box>
          )}

          <Stack direction="row" justifyContent="space-between" mt={4}>
            <Button variant="outlined" startIcon={<ArrowBackRounded />}
              disabled={currentIndex === 0}
              onClick={() => setCurrentIndex((i) => i - 1)}
              sx={{ borderRadius: 2 }}>
              Previous
            </Button>
            <Button variant="contained" endIcon={<ArrowForwardRounded />}
              disabled={currentIndex === questions.length - 1}
              onClick={() => setCurrentIndex((i) => i + 1)}
              sx={{ borderRadius: 2 }}>
              Next
            </Button>
          </Stack>

          <Box sx={{ textAlign: "center", mt: 4 }}>
            <Button variant="contained" color="error" size="large"
              onClick={() => setSubmitDialogOpen(true)}
              sx={{ borderRadius: 2, px: 6 }}>
              Submit Quiz
            </Button>
          </Box>
        </Box>
      </Box>

      <Dialog open={submitDialogOpen} onClose={() => setSubmitDialogOpen(false)} PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle><Typography fontWeight={700}>Submit Quiz?</Typography></DialogTitle>
        <DialogContent>
          <Typography>
            {answeredCount < questions.length
              ? `You have ${questions.length - answeredCount} unanswered question(s). Are you sure you want to submit?`
              : "Are you sure you want to submit your answers?"}
          </Typography>
          {markedQuestions.size > 0 && (
            <Typography variant="body2" color="warning.dark" mt={1}>
              {markedQuestions.size} question(s) are marked for review.
            </Typography>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button onClick={() => setSubmitDialogOpen(false)} variant="outlined">Cancel</Button>
          <Button onClick={handleSubmit} variant="contained" color="error">Submit</Button>
        </DialogActions>
      </Dialog>

      {submitting && (
        <Box sx={{ position: "fixed", inset: 0, bgcolor: "rgba(255,255,255,0.8)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 9999 }}>
          <CircularProgress />
        </Box>
      )}
    </Box>
  );
};

export default QuizAttemptPage;
