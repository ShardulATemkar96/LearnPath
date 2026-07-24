import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, CircularProgress, FormControl, InputLabel, MenuItem,
  Select, Stack, Typography,
} from "@mui/material";
import {
  ArrowBackRounded, LinkRounded, LinkOffRounded, QuizRounded,
} from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import { quizService as qs } from "../../services/quizService";
import {
  QuizResponseDto, ModuleQuizResponseDto, QuizStatus,
} from "../../types/quiz.types";

const AdminQuizEditorPage = () => {
  const { pathId, moduleId } = useParams<{ pathId: string; moduleId: string }>();
  const navigate = useNavigate();
  const mid = Number(moduleId);

  const [quiz, setQuiz] = useState<QuizResponseDto | null>(null);
  const [moduleQuiz, setModuleQuiz] = useState<ModuleQuizResponseDto | null>(null);
  const [allQuizzes, setAllQuizzes] = useState<QuizResponseDto[]>([]);
  const [selectedQuizId, setSelectedQuizId] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const load = async () => {
    setLoading(true);
    setError("");
    try {
      const [mq, quizzes] = await Promise.all([
        qs.getModuleQuiz(mid),
        qs.getAll(),
      ]);
      setModuleQuiz(mq);
      setAllQuizzes(quizzes.filter((q) => q.status === QuizStatus.Published));
      if (mq) {
        const q = await qs.getById(mq.quizId);
        setQuiz(q);
        setSelectedQuizId(q.id);
      } else {
        setQuiz(null);
        setSelectedQuizId(0);
      }
    } catch {
      setQuiz(null);
      setModuleQuiz(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!mid) return;
    load();
  }, [mid]);

  const handleLink = async () => {
    if (!selectedQuizId) return;
    setSaving(true);
    setError("");
    try {
      const mq = await quizService.linkQuiz(mid, selectedQuizId);
      setModuleQuiz(mq);
      const q = await qs.getById(mq.quizId);
      setQuiz(q);
    } catch (e: any) {
      setError(e?.response?.data?.message || "Failed to assign quiz.");
    } finally { setSaving(false); }
  };

  const handleUnlink = async () => {
    setSaving(true);
    setError("");
    try {
      await quizService.unlinkQuiz(mid);
      setQuiz(null);
      setModuleQuiz(null);
      setSelectedQuizId(0);
    } catch (e: any) {
      setError(e?.response?.data?.message || "Failed to unlink quiz.");
    } finally { setSaving(false); }
  };

  if (loading) return <Box sx={{ textAlign: "center", py: 8 }}><CircularProgress /></Box>;

  return (
    <Box>
      <Button startIcon={<ArrowBackRounded />} onClick={() => navigate(`/admin/paths/${pathId}/modules`)}
        sx={{ mb: 3, color: "text.secondary" }}>
        Back to Modules
      </Button>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError("")}>{error}</Alert>}

      <Stack direction="row" alignItems="center" spacing={1} mb={3}>
        <QuizRounded color="primary" />
        <Typography variant="h5" fontWeight={700}>Module Quiz Assignment</Typography>
      </Stack>

      {quiz && moduleQuiz ? (
        <Box sx={{ p: 3, borderRadius: 3, border: "1px solid", borderColor: "divider", bgcolor: "background.paper" }}>
          <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
            <Box>
              <Typography variant="subtitle2" color="text.secondary">Assigned Quiz</Typography>
              <Typography variant="h6" fontWeight={600}>{quiz.title}</Typography>
              <Stack direction="row" spacing={2} mt={1}>
                <Chip label={`${quiz.questionCount} questions`} size="small" />
                <Chip label={`Pass: ${quiz.passingPercentage}%`} size="small" />
                <Chip label={`Max: ${quiz.maximumAttempts} attempts`} size="small" />
                {quiz.timeLimitMinutes && <Chip label={`${quiz.timeLimitMinutes} min`} size="small" />}
              </Stack>
            </Box>
            <Button variant="outlined" color="error" startIcon={<LinkOffRounded />}
              onClick={handleUnlink} disabled={saving}>
              Unlink
            </Button>
          </Stack>
        </Box>
      ) : (
        <Box sx={{ p: 3, borderRadius: 3, border: "1px solid", borderColor: "divider", bgcolor: "background.paper" }}>
          <Typography variant="subtitle2" color="text.secondary" mb={2}>No quiz assigned to this module</Typography>
          <Stack direction="row" spacing={2} alignItems="center">
            <FormControl sx={{ minWidth: 300 }}>
              <InputLabel>Select Published Quiz</InputLabel>
              <Select value={selectedQuizId || ""} label="Select Published Quiz"
                onChange={(e) => setSelectedQuizId(Number(e.target.value))}>
                {allQuizzes.map((q) => (
                  <MenuItem key={q.id} value={q.id}>{q.title} ({q.questionBankTitle})</MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button variant="contained" startIcon={<LinkRounded />} onClick={handleLink}
              disabled={!selectedQuizId || saving}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              Assign
            </Button>
          </Stack>
          {allQuizzes.length === 0 && (
            <Typography variant="body2" color="text.secondary" mt={1}>
              No published quizzes available. Create and publish a quiz in{" "}
              <Typography component="span" color="primary" sx={{ cursor: "pointer" }}
                onClick={() => navigate("/admin/quizzes")}>Quiz Management</Typography> first.
            </Typography>
          )}
        </Box>
      )}
    </Box>
  );
};

export default AdminQuizEditorPage;
