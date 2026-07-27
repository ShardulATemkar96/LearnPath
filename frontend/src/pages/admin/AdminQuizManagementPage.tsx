import { useEffect, useState, useCallback } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogContent, DialogTitle,
  DialogActions, DialogContentText, FormControl, IconButton, InputLabel, MenuItem,
  Select, Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Typography, Paper,
} from "@mui/material";
import {
  AddRounded, ArchiveRounded, BarChartRounded, CheckCircleRounded, CloseRounded,
  DeleteRounded, EditRounded, QuizRounded, UndoRounded,
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
  const [deleteTarget, setDeleteTarget] = useState<QuizResponseDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [publishTarget, setPublishTarget] = useState<QuizResponseDto | null>(null);
  const [publishing, setPublishing] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

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

  const handlePublishConfirm = async () => {
    if (!publishTarget) return;
    setPublishing(true);
    setError("");
    try {
      await quizService.publish(publishTarget.id);
      setPublishTarget(null);
      setSuccessMsg(`"${publishTarget.title}" published successfully.`);
      loadQuizzes();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Publish failed.");
      setPublishTarget(null);
    } finally { setPublishing(false); }
  };

  const handleUnpublish = async (id: number) => {
    setError("");
    try {
      await quizService.unpublish(id);
      setSuccessMsg("Quiz unpublished.");
      loadQuizzes();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Unpublish failed.");
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    setError("");
    try {
      await quizService.delete(deleteTarget.id);
      setDeleteTarget(null);
      loadQuizzes();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Delete failed.");
      setDeleteTarget(null);
    } finally { setDeleting(false); }
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
      <Snackbar open={!!successMsg} autoHideDuration={3000} onClose={() => setSuccessMsg("")}
        message={successMsg} anchorOrigin={{ vertical: "bottom", horizontal: "center" }} />

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
                          {q.status === QuizStatus.Draft && (
                            <IconButton size="small" title="Publish" color="success"
                              onClick={() => setPublishTarget(q)}>
                              <CheckCircleRounded fontSize="small" />
                            </IconButton>
                          )}
                          {q.status === QuizStatus.Published && (
                            <IconButton size="small" title="Unpublish"
                              onClick={() => handleUnpublish(q.id)}>
                              <UndoRounded fontSize="small" />
                            </IconButton>
                          )}
                          <IconButton size="small" title="Archive" color="error" onClick={() => handleArchive(q.id)}>
                            <ArchiveRounded fontSize="small" />
                          </IconButton>
                        </>
                      )}
                      <IconButton size="small" title="Delete" color="error"
                        onClick={() => setDeleteTarget(q)}>
                        <DeleteRounded fontSize="small" />
                      </IconButton>
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

      <Dialog open={deleteTarget !== null} onClose={() => { if (!deleting) setDeleteTarget(null); }}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Typography variant="h6" fontWeight={700}>Delete Quiz</Typography>
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete '{deleteTarget?.title}'? This action cannot be undone and will permanently remove the quiz and all associated data.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setDeleteTarget(null)} disabled={deleting}
            sx={{ borderRadius: 2, color: "text.secondary" }}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleDelete} disabled={deleting}
            sx={{ borderRadius: 2 }}>
            {deleting ? "Deleting..." : "Delete"}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={publishTarget !== null} onClose={() => { if (!publishing) setPublishTarget(null); }}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Typography variant="h6" fontWeight={700}>Publish Quiz</Typography>
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            Once published, this quiz becomes available for Module Quiz Assignment and can later be assigned to students.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setPublishTarget(null)} disabled={publishing}
            sx={{ borderRadius: 2, color: "text.secondary" }}>Cancel</Button>
          <Button variant="contained" color="success" onClick={handlePublishConfirm} disabled={publishing}
            sx={{ borderRadius: 2 }}>
            {publishing ? "Publishing..." : "Publish"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AdminQuizManagementPage;
