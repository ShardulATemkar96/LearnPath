import { useEffect, useState, useCallback } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogContent, DialogTitle,
  FormControl, IconButton, InputLabel, MenuItem, Select, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Typography, Paper,
} from "@mui/material";
import {
  AddRounded, ArchiveRounded, BarChartRounded, CloseRounded, EditRounded, QuizRounded,
} from "@mui/icons-material";
import { quizService } from "../../services/quizService";
import { questionBankService } from "../../services/questionBankService";
import {
  CreateQuizDto, QuizResponseDto, QuizStatus, Difficulty, SelectionMode,
} from "../../types/quiz.types";
import { QuestionBankSummaryDto, QuestionBankStatus as QBStatus } from "../../types/questionBank.types";
import { useNavigate } from "react-router-dom";

const STATUS_LABEL: Record<QuizStatus, string> = {
  [QuizStatus.Draft]: "Draft",
  [QuizStatus.Published]: "Published",
  [QuizStatus.Archived]: "Archived",
};

const STATUS_COLOR: Record<QuizStatus, "default" | "success" | "error"> = {
  [QuizStatus.Draft]: "default",
  [QuizStatus.Published]: "success",
  [QuizStatus.Archived]: "error",
};

const emptyForm = (): CreateQuizDto => ({
  title: "", questionBankId: 0, questionCount: 5,
  difficultyFilter: undefined, selectionMode: SelectionMode.Random,
  timeLimitMinutes: undefined, passingPercentage: 40, maximumAttempts: 3,
});

const AdminQuizManagementPage = () => {
  const navigate = useNavigate();
  const [quizzes, setQuizzes] = useState<QuizResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editId, setEditId] = useState<number | null>(null);
  const [form, setForm] = useState<CreateQuizDto>(emptyForm());
  const [saving, setSaving] = useState(false);

  const [banks, setBanks] = useState<QuestionBankSummaryDto[]>([]);

  const loadQuizzes = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const data = await quizService.getAll();
      setQuizzes(data);
    } catch { setError("Failed to load quizzes."); }
    finally { setLoading(false); }
  }, []);

  const loadBanks = useCallback(async () => {
    try {
      const result = await questionBankService.search();
      setBanks(result.items.filter((b) => b.status !== QBStatus.Archived));
    } catch { }
  }, []);

  useEffect(() => { loadQuizzes(); loadBanks(); }, [loadQuizzes, loadBanks]);

  const openCreate = () => {
    setEditId(null);
    setForm(emptyForm());
    setDialogOpen(true);
  };

  const openEdit = (q: QuizResponseDto) => {
    setEditId(q.id);
    setForm({
      title: q.title, questionBankId: q.questionBankId, questionCount: q.questionCount,
      difficultyFilter: q.difficultyFilter ?? undefined,
      selectionMode: q.selectionMode, timeLimitMinutes: q.timeLimitMinutes,
      passingPercentage: q.passingPercentage, maximumAttempts: q.maximumAttempts,
    });
    setDialogOpen(true);
  };

  const handleSave = async () => {
    setSaving(true);
    setError("");
    try {
      if (editId) {
        await quizService.update(editId, form);
      } else {
        await quizService.create(form);
      }
      setDialogOpen(false);
      loadQuizzes();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Failed to save quiz.");
    } finally { setSaving(false); }
  };

  const handleArchive = async (id: number) => {
    setError("");
    try {
      await quizService.archive(id);
      loadQuizzes();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Archive failed.");
    }
  };

  const selectedBank = banks.find((b) => b.id === form.questionBankId);

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5" fontWeight={700}>Quiz Management</Typography>
        <Button variant="contained" startIcon={<AddRounded />} onClick={openCreate}
          sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
          Create Quiz
        </Button>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError("")}>{error}</Alert>}

      {loading ? (
        <Box sx={{ textAlign: "center", py: 8 }}><CircularProgress /></Box>
      ) : quizzes.length === 0 ? (
        <Box sx={{ textAlign: "center", py: 8, color: "text.secondary" }}>
          <QuizRounded sx={{ fontSize: 64, mb: 2, opacity: 0.3 }} />
          <Typography variant="h6">No quizzes created yet</Typography>
          <Typography variant="body2">Create a quiz to assign to modules.</Typography>
        </Box>
      ) : (
        <TableContainer component={Paper} sx={{ borderRadius: 2 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Question Bank</TableCell>
                <TableCell>Questions</TableCell>
                <TableCell>Difficulty</TableCell>
                <TableCell>Mode</TableCell>
                <TableCell>Pass %</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {quizzes.map((q) => (
                <TableRow key={q.id}>
                  <TableCell><Typography fontWeight={600}>{q.title}</Typography></TableCell>
                  <TableCell>{q.questionBankTitle} (v{q.questionBankVersion})</TableCell>
                  <TableCell>{q.questionCount}</TableCell>
                  <TableCell>{q.difficultyFilter !== null ? Difficulty[q.difficultyFilter] : "Any"}</TableCell>
                  <TableCell>{SelectionMode[q.selectionMode]}</TableCell>
                  <TableCell>{q.passingPercentage}%</TableCell>
                  <TableCell>
                    <Chip label={STATUS_LABEL[q.status]} color={STATUS_COLOR[q.status]} size="small" />
                  </TableCell>
                  <TableCell align="right">
                    <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                      <IconButton size="small" title="Analytics" onClick={() => navigate(`/admin/quiz-analytics?quizId=${q.id}`)}>
                        <BarChartRounded fontSize="small" />
                      </IconButton>
                      {q.status !== QuizStatus.Archived && (
                        <>
                          <IconButton size="small" title="Edit" onClick={() => openEdit(q)}>
                            <EditRounded fontSize="small" />
                          </IconButton>
                          <IconButton size="small" title="Archive" color="error" onClick={() => handleArchive(q.id)}>
                            <ArchiveRounded fontSize="small" />
                          </IconButton>
                        </>
                      )}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" fontWeight={700}>{editId ? "Edit Quiz" : "Create Quiz"}</Typography>
            <IconButton onClick={() => setDialogOpen(false)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField label="Quiz Title" fullWidth value={form.title}
              onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} />

            <FormControl fullWidth>
              <InputLabel>Question Bank</InputLabel>
              <Select value={form.questionBankId || ""} label="Question Bank"
                onChange={(e) => {
                  const bankId = Number(e.target.value);
                  const bank = banks.find((b) => b.id === bankId);
                  setForm((p) => ({ ...p, questionBankId: bankId, questionCount: bank ? Math.min(p.questionCount, bank.questionCount) : 5 }));
                }}>
                {banks.map((b) => (
                  <MenuItem key={b.id} value={b.id}>{b.title} (v{b.version} - {b.questionCount} Q)</MenuItem>
                ))}
              </Select>
            </FormControl>

            {selectedBank && (
              <Typography variant="caption" color="text.secondary">
                Available questions: {selectedBank.questionCount}
              </Typography>
            )}

            <TextField label="Question Count" type="number" fullWidth value={form.questionCount}
              onChange={(e) => setForm((p) => ({ ...p, questionCount: parseInt(e.target.value) || 1 }))}
              inputProps={{ min: 1, max: selectedBank?.questionCount || 999 }} />

            <FormControl fullWidth>
              <InputLabel>Difficulty Filter (optional)</InputLabel>
              <Select value={form.difficultyFilter ?? ""} label="Difficulty Filter (optional)"
                onChange={(e) => setForm((p) => ({ ...p, difficultyFilter: e.target.value === "" ? undefined : Number(e.target.value) as Difficulty }))}>
                <MenuItem value="">All Difficulties</MenuItem>
                <MenuItem value={Difficulty.Easy}>Easy</MenuItem>
                <MenuItem value={Difficulty.Medium}>Medium</MenuItem>
                <MenuItem value={Difficulty.Hard}>Hard</MenuItem>
              </Select>
            </FormControl>

            <FormControl fullWidth>
              <InputLabel>Selection Mode</InputLabel>
              <Select value={form.selectionMode} label="Selection Mode"
                onChange={(e) => setForm((p) => ({ ...p, selectionMode: Number(e.target.value) as SelectionMode }))}>
                <MenuItem value={SelectionMode.Random}>Random</MenuItem>
                <MenuItem value={SelectionMode.Sequential}>Sequential</MenuItem>
              </Select>
            </FormControl>

            <TextField label="Time Limit (minutes, optional)" type="number" fullWidth
              value={form.timeLimitMinutes ?? ""}
              onChange={(e) => setForm((p) => ({ ...p, timeLimitMinutes: e.target.value ? parseInt(e.target.value) : undefined }))} />

            <TextField label="Passing Percentage" type="number" fullWidth value={form.passingPercentage}
              onChange={(e) => setForm((p) => ({ ...p, passingPercentage: parseInt(e.target.value) || 40 }))} />

            <TextField label="Maximum Attempts" type="number" fullWidth value={form.maximumAttempts}
              onChange={(e) => setForm((p) => ({ ...p, maximumAttempts: parseInt(e.target.value) || 3 }))}
              inputProps={{ min: 1 }} />

            <Button variant="contained" fullWidth size="large" onClick={handleSave} disabled={saving || !form.title || !form.questionBankId}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              {editId ? "Save Changes" : "Create Quiz"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default AdminQuizManagementPage;
