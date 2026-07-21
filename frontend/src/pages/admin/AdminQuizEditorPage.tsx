import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogContent, DialogTitle,
  Divider, FormControl, IconButton, InputLabel, MenuItem, Select, Stack,
  Switch, TextField, Typography,
} from "@mui/material";
import { AddRounded, ArrowBackRounded, CloseRounded, DeleteRounded, EditRounded, QuizRounded } from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import {
  AdminOptionDto, AdminQuestionDto, CreateOptionRequest, CreateQuestionRequest, CreateQuizRequest,
  QuizQuestionType, QuizResponseDto,
} from "../../types/quiz.types";

const emptyQuestionForm = (order: number): CreateQuestionRequest => ({
  questionText: "", questionType: QuizQuestionType.MultipleChoice,
  points: 1, orderIndex: order, explanation: "", options: [
    { optionText: "True", isCorrect: false, orderIndex: 0 },
    { optionText: "False", isCorrect: false, orderIndex: 1 },
  ],
});

const AdminQuizEditorPage = () => {
  const { pathId, moduleId } = useParams<{ pathId: string; moduleId: string }>();
  const navigate = useNavigate();
  const mid = Number(moduleId);

  const [quiz, setQuiz] = useState<QuizResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [settingsOpen, setSettingsOpen] = useState(false);
  const [settingsForm, setSettingsForm] = useState<CreateQuizRequest>({
    title: "", description: "", passingScore: 0, timeLimitMinutes: undefined,
    maxAttempts: 0, shuffleQuestions: false, showResults: true, isMandatory: false,
  });

  const [qDialogOpen, setQDialogOpen] = useState(false);
  const [editQuestion, setEditQuestion] = useState<AdminQuestionDto | null>(null);
  const [qForm, setQForm] = useState<CreateQuestionRequest>(emptyQuestionForm(0));

  const loadQuiz = async () => {
    setLoading(true);
    try {
      const q = await quizService.getQuiz(mid);
      setQuiz(q);
    } catch {
      setQuiz(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!mid) return;
    loadQuiz();
  }, [mid]);

  const openCreateQuiz = () => {
    setSettingsForm({
      title: "", description: "", passingScore: 0, timeLimitMinutes: undefined,
      maxAttempts: 0, shuffleQuestions: false, showResults: true, isMandatory: false,
    });
    setSettingsOpen(true);
  };

  const openEditQuiz = () => {
    if (!quiz) return;
    setSettingsForm({
      title: quiz.title, description: quiz.description ?? "",
      passingScore: quiz.passingScore ?? 0, timeLimitMinutes: quiz.timeLimitMinutes,
      maxAttempts: quiz.maxAttempts, shuffleQuestions: quiz.shuffleQuestions,
      showResults: quiz.showResults, isMandatory: quiz.isMandatory,
    });
    setSettingsOpen(true);
  };

  const saveSettings = async () => {
    try {
      if (quiz) {
        const updated = await quizService.updateQuiz(mid, quiz.id, settingsForm);
        setQuiz(updated);
      } else {
        const created = await quizService.createQuiz(mid, settingsForm);
        setQuiz(created);
      }
      setSettingsOpen(false);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to save quiz settings.");
    }
  };

  const handleDeleteQuiz = async () => {
    if (!quiz) return;
    try {
      await quizService.deleteQuiz(mid, quiz.id);
      setQuiz(null);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to delete quiz.");
    }
  };

  const openAddQuestion = () => {
    const order = quiz ? quiz.questions.length : 0;
    setEditQuestion(null);
    setQForm({
      questionText: "", questionType: QuizQuestionType.MultipleChoice,
      points: 1, orderIndex: order, explanation: "",
      options: [
        { optionText: "", isCorrect: false, orderIndex: 0 },
        { optionText: "", isCorrect: false, orderIndex: 1 },
      ],
    });
    setQDialogOpen(true);
  };

  const openEditQuestion = (q: AdminQuestionDto) => {
    setEditQuestion(q);
    setQForm({
      questionText: q.questionText, questionType: q.questionType,
      points: q.points, orderIndex: q.orderIndex, explanation: q.explanation ?? "",
      options: q.options.map((o: AdminOptionDto) => ({
        optionText: o.optionText, isCorrect: o.isCorrect, orderIndex: o.orderIndex,
      })),
    });
    setQDialogOpen(true);
  };

  const saveQuestion = async () => {
    if (!quiz) return;
    try {
      if (editQuestion) {
        const updated = await quizService.updateQuestion(mid, quiz.id, editQuestion.id, qForm);
        setQuiz((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            questions: prev.questions.map((q) => q.id === updated.id ? updated : q),
          };
        });
      } else {
        const created = await quizService.addQuestion(mid, quiz.id, qForm);
        setQuiz((prev) => {
          if (!prev) return prev;
          return { ...prev, questions: [...prev.questions, created] };
        });
      }
      setQDialogOpen(false);
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to save question.");
    }
  };

  const handleDeleteQuestion = async (questionId: number) => {
    if (!quiz) return;
    try {
      await quizService.deleteQuestion(mid, quiz.id, questionId);
      setQuiz((prev) => {
        if (!prev) return prev;
        return { ...prev, questions: prev.questions.filter((q) => q.id !== questionId) };
      });
    } catch (e: any) {
      setError(e?.response?.data?.message ?? "Failed to delete question.");
    }
  };

  if (loading) return <Box sx={{ textAlign: "center", py: 8 }}><CircularProgress /></Box>;

  return (
    <Box>
      <Button startIcon={<ArrowBackRounded />} onClick={() => navigate(`/admin/paths/${pathId}/modules`)}
        sx={{ mb: 3, color: "text.secondary" }}>
        Back to Modules
      </Button>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError("")}>{error}</Alert>}

      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Stack direction="row" alignItems="center" spacing={1}>
          <QuizRounded color="primary" />
          <Typography variant="h5" fontWeight={700}>Quiz Editor</Typography>
        </Stack>
        <Stack direction="row" spacing={1}>
          {quiz && <Button variant="outlined" color="error" onClick={handleDeleteQuiz}>Delete Quiz</Button>}
          {quiz ? (
            <Button variant="outlined" onClick={openEditQuiz}>Settings</Button>
          ) : (
            <Button variant="contained" startIcon={<AddRounded />} onClick={openCreateQuiz}>Create Quiz</Button>
          )}
        </Stack>
      </Stack>

      {!quiz && (
        <Box sx={{ textAlign: "center", py: 8, color: "text.secondary" }}>
          <QuizRounded sx={{ fontSize: 64, mb: 2, opacity: 0.3 }} />
          <Typography variant="h6">No quiz configured</Typography>
          <Typography variant="body2">Click "Create Quiz" to get started.</Typography>
        </Box>
      )}

      {quiz && (
        <>
          <Box sx={{ p: 3, borderRadius: 3, border: "1px solid", borderColor: "divider", mb: 3, bgcolor: "background.paper" }}>
            <Stack direction="row" spacing={3} alignItems="center" flexWrap="wrap">
              <Box><Typography variant="caption" color="text.secondary">Title</Typography><Typography fontWeight={600}>{quiz.title}</Typography></Box>
              <Box><Typography variant="caption" color="text.secondary">Questions</Typography><Typography fontWeight={600}>{quiz.questions.length}</Typography></Box>
              <Box><Typography variant="caption" color="text.secondary">Passing Score</Typography><Typography fontWeight={600}>{quiz.passingScore ?? 0}%</Typography></Box>
              {quiz.timeLimitMinutes && <Box><Typography variant="caption" color="text.secondary">Time Limit</Typography><Typography fontWeight={600}>{quiz.timeLimitMinutes} min</Typography></Box>}
              <Box><Typography variant="caption" color="text.secondary">Shuffle</Typography><Chip label={quiz.shuffleQuestions ? "Yes" : "No"} size="small" /></Box>
              <Box><Typography variant="caption" color="text.secondary">Mandatory</Typography><Chip label={quiz.isMandatory ? "Yes" : "No"} size="small" /></Box>
              {quiz.isPublished && <Chip label="Published" color="success" size="small" />}
            </Stack>
          </Box>

          <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h6" fontWeight={600}>Questions</Typography>
            <Button variant="contained" startIcon={<AddRounded />} onClick={openAddQuestion}>Add Question</Button>
          </Stack>

          {quiz.questions.length === 0 && (
            <Typography color="text.secondary" textAlign="center" py={4}>No questions yet.</Typography>
          )}

          {quiz.questions.map((q, i) => (
            <Box key={q.id || i} sx={{ p: 2, mb: 1.5, borderRadius: 2, border: "1px solid", borderColor: "divider", bgcolor: "background.paper" }}>
              <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
                <Box sx={{ flex: 1 }}>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <Chip label={`Q${i + 1}`} size="small" color="primary" variant="outlined" />
                    <Chip label={["Multiple Choice", "True/False", "Short Answer"][q.questionType]} size="small" />
                    <Chip label={`${q.points} pt${q.points !== 1 ? "s" : ""}`} size="small" variant="outlined" />
                  </Stack>
                  <Typography fontWeight={600}>{q.questionText}</Typography>
                  {q.options.map((o, j) => (
                    <Typography key={j} variant="body2" color={o.isCorrect ? "success.main" : "text.secondary"} sx={{ ml: 2 }}>
                      {o.isCorrect ? "✓ " : ""}{o.optionText}
                    </Typography>
                  ))}
                  {q.explanation && <Typography variant="caption" color="text.secondary" fontStyle="italic">{q.explanation}</Typography>}
                </Box>
                <Stack direction="row" spacing={0.5}>
                  <IconButton size="small" onClick={() => openEditQuestion(q)}><EditRounded fontSize="small" /></IconButton>
                  <IconButton size="small" color="error" onClick={() => handleDeleteQuestion(q.id)}><DeleteRounded fontSize="small" /></IconButton>
                </Stack>
              </Stack>
            </Box>
          ))}
        </>
      )}

      <Dialog open={settingsOpen} onClose={() => setSettingsOpen(false)} maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" fontWeight={700}>{quiz ? "Quiz Settings" : "Create Quiz"}</Typography>
            <IconButton onClick={() => setSettingsOpen(false)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField label="Title" fullWidth value={settingsForm.title}
              onChange={(e) => setSettingsForm((p) => ({ ...p, title: e.target.value }))} />
            <TextField label="Description" fullWidth multiline rows={2} value={settingsForm.description ?? ""}
              onChange={(e) => setSettingsForm((p) => ({ ...p, description: e.target.value }))} />
            <TextField label="Passing Score" type="number" fullWidth value={settingsForm.passingScore}
              onChange={(e) => setSettingsForm((p) => ({ ...p, passingScore: parseInt(e.target.value) || 0 }))} />
            <TextField label="Time Limit (minutes)" type="number" fullWidth value={settingsForm.timeLimitMinutes ?? ""}
              onChange={(e) => setSettingsForm((p) => ({ ...p, timeLimitMinutes: e.target.value ? parseInt(e.target.value) : undefined }))} />
            <TextField label="Max Attempts (0 = unlimited)" type="number" fullWidth value={settingsForm.maxAttempts}
              onChange={(e) => setSettingsForm((p) => ({ ...p, maxAttempts: parseInt(e.target.value) || 0 }))} />
            <Stack direction="row" alignItems="center"><Switch checked={settingsForm.shuffleQuestions} onChange={(e) => setSettingsForm((p) => ({ ...p, shuffleQuestions: e.target.checked }))} /><Typography variant="body2">Shuffle Questions</Typography></Stack>
            <Stack direction="row" alignItems="center"><Switch checked={settingsForm.showResults} onChange={(e) => setSettingsForm((p) => ({ ...p, showResults: e.target.checked }))} /><Typography variant="body2">Show Results</Typography></Stack>
            <Stack direction="row" alignItems="center"><Switch checked={settingsForm.isMandatory} onChange={(e) => setSettingsForm((p) => ({ ...p, isMandatory: e.target.checked }))} /><Typography variant="body2">Mandatory</Typography></Stack>
            <Button variant="contained" fullWidth size="large" onClick={saveSettings}>Save</Button>
          </Stack>
        </DialogContent>
      </Dialog>

      <Dialog open={qDialogOpen} onClose={() => setQDialogOpen(false)} maxWidth="md" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" fontWeight={700}>{editQuestion ? "Edit Question" : "Add Question"}</Typography>
            <IconButton onClick={() => setQDialogOpen(false)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField label="Question Text" fullWidth multiline rows={2} value={qForm.questionText}
              onChange={(e) => setQForm((p) => ({ ...p, questionText: e.target.value }))} />

            <FormControl fullWidth>
              <InputLabel>Question Type</InputLabel>
              <Select value={qForm.questionType} label="Question Type"
                onChange={(e) => setQForm((p) => ({ ...p, questionType: Number(e.target.value) as QuizQuestionType }))}>
                <MenuItem value={QuizQuestionType.MultipleChoice}>Multiple Choice</MenuItem>
                <MenuItem value={QuizQuestionType.TrueFalse}>True / False</MenuItem>
                <MenuItem value={QuizQuestionType.ShortAnswer}>Short Answer</MenuItem>
              </Select>
            </FormControl>

            <TextField label="Points" type="number" fullWidth value={qForm.points}
              onChange={(e) => setQForm((p) => ({ ...p, points: parseInt(e.target.value) || 1 }))} />

            <TextField label="Explanation (shown after quiz)" fullWidth multiline rows={2} value={qForm.explanation ?? ""}
              onChange={(e) => setQForm((p) => ({ ...p, explanation: e.target.value }))} />

            <Divider />
            <Typography variant="subtitle2" fontWeight={600}>Options</Typography>

            {qForm.questionType === QuizQuestionType.TrueFalse && (
              <Typography variant="body2" color="text.secondary">True/False questions use two default options. Mark the correct one below.</Typography>
            )}

            {qForm.options.map((opt, i) => (
              <Stack key={i} direction="row" spacing={1} alignItems="center">
                <TextField size="small" label={`Option ${i + 1}`} value={opt.optionText} sx={{ flex: 1 }}
                  onChange={(e) => {
                    const updated = [...qForm.options];
                    updated[i] = { ...updated[i], optionText: e.target.value };
                    setQForm((p) => ({ ...p, options: updated }));
                  }} />
                <Switch
                  checked={opt.isCorrect}
                  onChange={(e) => {
                    const updated = qForm.options.map((o, j) => ({
                      ...o, isCorrect: qForm.questionType === QuizQuestionType.MultipleChoice ? (j === i ? e.target.checked : false) : j === i ? e.target.checked : false,
                    }));
                    setQForm((p) => ({ ...p, options: updated }));
                  }} />
                <Typography variant="caption" color={opt.isCorrect ? "success.main" : "text.secondary"} sx={{ minWidth: 50 }}>
                  {opt.isCorrect ? "Correct" : "Wrong"}
                </Typography>
                {qForm.questionType !== QuizQuestionType.TrueFalse && (
                  <IconButton size="small" color="error" onClick={() => {
                    if (qForm.options.length <= 1) return;
                    setQForm((p) => ({ ...p, options: p.options.filter((_, j) => j !== i) }));
                  }}><DeleteRounded fontSize="small" /></IconButton>
                )}
              </Stack>
            ))}

            {qForm.questionType !== QuizQuestionType.TrueFalse && (
              <Button variant="outlined" size="small" startIcon={<AddRounded />}
                onClick={() => setQForm((p) => ({
                  ...p,
                  options: [...p.options, { optionText: "", isCorrect: false, orderIndex: p.options.length }],
                }))}>
                Add Option
              </Button>
            )}

            <Divider />
            <Button variant="contained" fullWidth size="large" onClick={saveQuestion}>
              {editQuestion ? "Save Changes" : "Add Question"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default AdminQuizEditorPage;
