import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, Dialog, DialogContent,
  DialogTitle, Divider, Grid, IconButton, Skeleton,
  Stack, Tab, Tabs, TextField, Typography,
} from "@mui/material";
import {
  ArrowBackRounded, CloseRounded,
  ContentCopyRounded, AddRounded,
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
import { Assignment } from "../../types/classroom.types";
import { classroomService } from "../../services/classroomService";

const SubmitModal = ({
  open, onClose, classroomId, assignment,
}: {
  open: boolean; onClose: () => void;
  classroomId: number; assignment: Assignment | null;
}) => {
  const [url, setUrl]       = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError]   = useState("");

  const handleSubmit = async () => {
    if (!url.trim()) { setError("Submission URL is required."); return; }
    if (!assignment) return;
    setLoading(true);
    try {
      await classroomService.submit(classroomId, assignment.id, url);
      onClose(); setUrl("");
    } catch (e: any) {
      setError(e.response?.data?.message ?? "Submission failed.");
    } finally { setLoading(false); }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>Submit Assignment</Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          {assignment && (
            <Typography variant="body2" color="text.secondary">
              {assignment.title}
            </Typography>
          )}
          {error && <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>}
          <TextField label="Submission URL" fullWidth value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://github.com/your-repo" />
          <Button variant="contained" fullWidth size="large"
            onClick={handleSubmit} disabled={loading}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            {loading ? "Submitting..." : "Submit"}
          </Button>
        </Stack>
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
  const [submitModal, setSubmitModal]         = useState(false);
  const [activeAssignment, setActiveAssignment] = useState<Assignment | null>(null);
  const [copied, setCopied]       = useState(false);

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

  const handleSubmitOpen = (assignment: Assignment) => {
    setActiveAssignment(assignment);
    setSubmitModal(true);
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

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 3 }}>
        <Tab label={`Assignments (${classroom.assignments.length})`} />
        <Tab label={`Members (${classroom.members.length})`} />
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
                isInstructor={isInstructor}
                onSubmit={handleSubmitOpen}
              />
            ))}
          </Stack>
        </Box>
      )}

      {tab === 1 && (
        <Stack spacing={2}>
          {classroom.members.map((m) => (
            <Box key={m.userId} sx={{ p: 2, border: "1px solid #ddd", borderRadius: 2 }}>
              <Typography>{m.fullName}</Typography>
              <Typography variant="caption">{m.email}</Typography>
            </Box>
          ))}
        </Stack>
      )}

      <CreateAssignmentModal
        open={assignmentModal}
        onClose={() => setAssignmentModal(false)}
        classroomId={classroom.id}
      />

      <SubmitModal
        open={submitModal}
        onClose={() => setSubmitModal(false)}
        classroomId={classroom.id}
        assignment={activeAssignment}
      />
    </Box>
  );
};

export default ClassroomDetailPage;
