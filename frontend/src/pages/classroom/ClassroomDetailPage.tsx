import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, Dialog, DialogContent,
  DialogTitle, Divider, Grid, IconButton, Skeleton,
  Stack, Tab, Tabs, TextField, Typography,
} from "@mui/material";
import {
  ArrowBackRounded, CloseRounded,
  ContentCopyRounded, AddRounded, PersonRemoveRounded,
  CheckCircleRounded, GradingRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchClassroomById, clearSelectedClassroom,
  createAssignmentThunk,
} from "../../redux/slices/classroomSlice";
import {
  selectSelectedClassroom,
  selectClassroomDetailLoading,
  selectClassroomError,
} from "../../redux/selectors/classroomSelectors";
import { ROUTES } from "../../constants/routes";
import AssignmentCard from "../../components/classroom/AssignmentCard/AssignmentCard";
import { Assignment, Submission } from "../../types/classroom.types";
import { classroomService } from "../../services/classroomService";

const SubmissionsPanel = ({
  open, onClose, classroomId, assignment,
}: {
  open: boolean; onClose: () => void;
  classroomId: number; assignment: Assignment | null;
}) => {
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [loading, setLoading] = useState(false);
  const [gradeInputs, setGradeInputs] = useState<Record<number, { grade: string; feedback: string }>>({});

  useEffect(() => {
    if (open && assignment) {
      setLoading(true);
      classroomService.getSubmissions(classroomId, assignment.id)
        .then(setSubmissions)
        .catch(() => {})
        .finally(() => setLoading(false));
    }
  }, [open, assignment, classroomId]);

  const handleVerify = async (subId: number) => {
    if (!assignment) return;
    await classroomService.verifySubmission(classroomId, assignment.id, subId);
    const updated = await classroomService.getSubmissions(classroomId, assignment.id);
    setSubmissions(updated);
  };

  const handleGrade = async (subId: number) => {
    if (!assignment) return;
    const inp = gradeInputs[subId];
    if (!inp || !inp.grade) return;
    await classroomService.gradeSubmission(classroomId, assignment.id, subId, parseInt(inp.grade), inp.feedback);
    const updated = await classroomService.getSubmissions(classroomId, assignment.id);
    setSubmissions(updated);
  };

  const handleComplete = async (subId: number) => {
    if (!assignment) return;
    await classroomService.completeSubmission(classroomId, assignment.id, subId);
    const updated = await classroomService.getSubmissions(classroomId, assignment.id);
    setSubmissions(updated);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>
            Submissions {assignment ? `— ${assignment.title}` : ""}
          </Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        {loading ? (
          <Typography>Loading...</Typography>
        ) : submissions.length === 0 ? (
          <Typography color="text.secondary">No submissions yet.</Typography>
        ) : (
          <Stack spacing={2} pt={1}>
            {submissions.map((s) => {
              const inp = gradeInputs[s.id] || { grade: s.grade?.toString() || "", feedback: s.feedback || "" };
              return (
                <Box key={s.id} sx={{ p: 2, border: "1px solid #ddd", borderRadius: 2 }}>
                  <Stack spacing={1.5}>
                    <Stack direction="row" justifyContent="space-between" alignItems="center">
                      <Box>
                        <Typography fontWeight={600}>{s.userFullName}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          <a href={s.contentUrl} target="_blank" rel="noopener noreferrer">{s.contentUrl}</a>
                        </Typography>
                      </Box>
                      <Chip label={s.status} size="small"
                        color={s.status === "Completed" ? "success" : s.status === "Resubmit" ? "warning" : "default"}
                        variant="outlined" />
                    </Stack>

                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                      {s.status === "Pending" && (
                        <Button size="small" variant="outlined" color="info"
                          onClick={() => handleVerify(s.id)}>
                          Verify
                        </Button>
                      )}
                      <TextField size="small" label="Grade (0-10)" type="number"
                        value={inp.grade} sx={{ width: 120 }}
                        onChange={(e) => setGradeInputs(p => ({
                          ...p, [s.id]: { ...p[s.id] || { grade: "", feedback: "" }, grade: e.target.value }
                        }))} />
                      <TextField size="small" label="Remarks" value={inp.feedback} sx={{ width: 200 }}
                        onChange={(e) => setGradeInputs(p => ({
                          ...p, [s.id]: { ...p[s.id] || { grade: "", feedback: "" }, feedback: e.target.value }
                        }))} />
                      <Button size="small" variant="contained" color="primary"
                        onClick={() => handleGrade(s.id)} disabled={!inp.grade}>
                        Grade
                      </Button>
                      {s.status === "Verified" && (
                        <Button size="small" variant="contained" color="success"
                          startIcon={<CheckCircleRounded />}
                          onClick={() => handleComplete(s.id)}>
                          Complete
                        </Button>
                      )}
                    </Stack>
                  </Stack>
                </Box>
              );
            })}
          </Stack>
        )}
      </DialogContent>
    </Dialog>
  );
};

const CreateAssignmentModal = ({
  open, onClose, classroomId,
}: { open: boolean; onClose: () => void; classroomId: number }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [form, setForm] = useState({ title: "", description: "", dueDate: "" });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    if (!form.title.trim() || !form.dueDate) return;
    setLoading(true);
    await dispatch(createAssignmentThunk({ classroomId, payload: form }));
    setLoading(false);
    setForm({ title: "", description: "", dueDate: "" });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>New Assignment</Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          <TextField label="Title" fullWidth value={form.title}
            onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} />
          <TextField label="Description" fullWidth multiline rows={2}
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} />
          <TextField label="Due Date" type="datetime-local" fullWidth
            value={form.dueDate}
            onChange={(e) => setForm((p) => ({ ...p, dueDate: e.target.value }))}
            InputLabelProps={{ shrink: true }} />
          <Button variant="contained" fullWidth size="large"
            onClick={handleSubmit} disabled={loading}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            {loading ? "Creating..." : "Create Assignment"}
          </Button>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

const ClassroomDetailPage = () => {
  const { id }      = useParams<{ id: string }>();
  const dispatch    = useDispatch<AppDispatch>();
  const navigate    = useNavigate();

  const classroom = useSelector(selectSelectedClassroom);
  const loading   = useSelector(selectClassroomDetailLoading);
  const error     = useSelector(selectClassroomError);

  const [tab, setTab]             = useState(0);
  const [assignmentModal, setAssignmentModal] = useState(false);
  const [submissionsPanel, setSubmissionsPanel] = useState(false);
  const [activeAssignment, setActiveAssignment] = useState<Assignment | null>(null);
  const [copied, setCopied]       = useState(false);

  const refresh = useCallback(() => {
    if (id) dispatch(fetchClassroomById(Number(id)));
  }, [id, dispatch]);

  useEffect(() => {
    if (id) dispatch(fetchClassroomById(Number(id)));
    return () => { dispatch(clearSelectedClassroom()); };
  }, [id, dispatch]);

  const isInstructor = classroom?.userRole === "Instructor";

  const handleCopyCode = () => {
    if (classroom?.inviteCode) {
      navigator.clipboard.writeText(classroom.inviteCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleViewSubmissions = (assignment: Assignment) => {
    setActiveAssignment(assignment);
    setSubmissionsPanel(true);
  };

  if (loading) return (
    <Box>
      <Skeleton height={40} width={200} sx={{ mb: 2 }} />
      <Skeleton variant="rounded" height={160} sx={{ borderRadius: 4, mb: 3 }} />
      <Skeleton variant="rounded" height={300} sx={{ borderRadius: 4 }} />
    </Box>
  );

  if (error) return <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>;
  if (!classroom) return null;

  return (
    <Box>
      <Button startIcon={<ArrowBackRounded />}
        onClick={() => navigate(ROUTES.CLASSROOM)}
        sx={{ mb: 3, color: "text.secondary" }}>
        Classrooms
      </Button>

      <Box sx={{
        p: 4, borderRadius: 4, mb: 4,
        background: "linear-gradient(135deg, #6C63FF18 0%, #9D97FF10 100%)",
        border: "1px solid rgba(108,99,255,0.12)",
      }}>
        <Stack spacing={2}>
          <Stack direction="row" alignItems="center" spacing={1.5} flexWrap="wrap">
            <Chip
              label={classroom.userRole}
              size="small"
              color={classroom.userRole === "Instructor" ? "primary" : "secondary"}
              sx={{ fontWeight: 600 }}
            />
            <Typography variant="caption" color="text.secondary">
              {classroom.learningPathTitle}
            </Typography>
          </Stack>

          <Typography variant="h4" fontWeight={700}>{classroom.title}</Typography>
          <Typography variant="body1" color="text.secondary">{classroom.description}</Typography>

          <Divider />

          <Stack direction="row" alignItems="center" spacing={3} flexWrap="wrap">
            <Stack direction="row" alignItems="center" spacing={1}>
              <Typography variant="body2" color="text.secondary">Invite Code:</Typography>
              <Chip
                label={classroom.inviteCode}
                size="small"
                sx={{ fontWeight: 700, letterSpacing: 2, fontFamily: "monospace" }}
              />
              <IconButton size="small" onClick={handleCopyCode}>
                <ContentCopyRounded sx={{ fontSize: 16, color: copied ? "success.main" : "text.secondary" }} />
              </IconButton>
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {classroom.memberCount} members
            </Typography>
          </Stack>
        </Stack>
      </Box>

      <Tabs value={isInstructor ? tab : 0} onChange={(_, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label={`Assignments (${classroom.assignments.length})`} />
        {isInstructor && <Tab label={`Members (${classroom.members.length})`} />}
      </Tabs>

      {tab === 0 && (
        <Box>
          {isInstructor && (
            <Button variant="contained" startIcon={<AddRounded />}
              onClick={() => setAssignmentModal(true)}
              sx={{ mb: 2 }}>
              Add Assignment
            </Button>
          )}

          <Stack spacing={2}>
            {classroom.assignments.map((a) => (
              <AssignmentCard
                key={a.id}
                assignment={a}
                classroomId={classroom.id}
                isInstructor={isInstructor}
                onRefresh={refresh}
                onViewSubmissions={handleViewSubmissions}
              />
            ))}
          </Stack>
        </Box>
      )}

      {tab === 1 && isInstructor && (
        <Stack spacing={2}>
          {classroom.members.map((m) => (
            <Box key={m.userId} sx={{ p: 2, border: "1px solid #ddd", borderRadius: 2 }}>
              <Stack direction="row" alignItems="center" justifyContent="space-between">
                <Box>
                  <Typography>{m.fullName}</Typography>
                  <Typography variant="caption">{m.email} — {m.role}</Typography>
                </Box>
                {m.role !== "Instructor" && (
                  <IconButton size="small" color="error"
                    onClick={async () => {
                      if (window.confirm(`Remove ${m.fullName} from this classroom?`)) {
                        await classroomService.removeMember(classroom.id, m.userId);
                        dispatch(fetchClassroomById(classroom.id));
                      }
                    }}
                    title="Remove member">
                    <PersonRemoveRounded />
                  </IconButton>
                )}
              </Stack>
            </Box>
          ))}
        </Stack>
      )}

      <CreateAssignmentModal
        open={assignmentModal}
        onClose={() => setAssignmentModal(false)}
        classroomId={classroom.id}
      />

      <SubmissionsPanel
        open={submissionsPanel}
        onClose={() => { setSubmissionsPanel(false); refresh(); }}
        classroomId={classroom.id}
        assignment={activeAssignment}
      />
    </Box>
  );
};

export default ClassroomDetailPage;
