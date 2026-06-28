import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Dialog, DialogContent, DialogTitle,
  Grid, IconButton, Skeleton, Stack, TextField, Typography,
} from "@mui/material";
import { AddRounded, LoginRounded, CloseRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchMyClassrooms, createClassroomThunk, joinClassroomThunk,
} from "../../redux/slices/classroomSlice";
import {
  selectClassrooms, selectClassroomLoading, selectClassroomError,
} from "../../redux/selectors/classroomSelectors";
import { selectMyPaths } from "../../redux/selectors/pathSelectors";
import { fetchMyPaths } from "../../redux/slices/pathSlice";
import ClassroomCard from "../../components/classroom/ClassroomCard/ClassroomCard";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import { GroupsRounded } from "@mui/icons-material";

const CreateClassroomModal = ({
  open, onClose,
}: { open: boolean; onClose: () => void }) => {
  const dispatch = useDispatch<AppDispatch>();
  const myPaths  = useSelector(selectMyPaths);
  const [form, setForm] = useState({ title: "", description: "", learningPathId: 0 });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async () => {
    if (!form.title.trim() || !form.learningPathId) {
      setError("Title and learning path are required."); return;
    }
    setError(""); setLoading(true);
    await dispatch(createClassroomThunk(form));
    setLoading(false);
    setForm({ title: "", description: "", learningPathId: 0 });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>New Classroom</Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          {error && <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>}
          <TextField label="Classroom Title" fullWidth value={form.title}
            onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))} />
          <TextField label="Description" fullWidth multiline rows={2}
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))} />
          <TextField select label="Learning Path" fullWidth
            value={form.learningPathId}
            onChange={(e) => setForm((p) => ({ ...p, learningPathId: Number(e.target.value) }))}
            SelectProps={{ native: true }}
          >
            <option value={0}>Select a path</option>
            {myPaths.map((p) => (
              <option key={p.id} value={p.id}>{p.title}</option>
            ))}
          </TextField>
          <Button variant="contained" fullWidth size="large" onClick={handleSubmit}
            disabled={loading}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            {loading ? "Creating..." : "Create Classroom"}
          </Button>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

const JoinClassroomModal = ({
  open, onClose,
}: { open: boolean; onClose: () => void }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [code, setCode]     = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError]   = useState("");

  const handleJoin = async () => {
    if (!code.trim()) { setError("Enter an invite code."); return; }
    setError(""); setLoading(true);
    const result = await dispatch(joinClassroomThunk(code.trim().toUpperCase()));
    if ((result as any).meta?.requestStatus === "fulfilled") {
      await dispatch(fetchMyClassrooms());
      onClose(); setCode("");
    } else {
      setError((result as any).payload ?? "Invalid code.");
    }
    setLoading(false);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>Join Classroom</Typography>
          <IconButton onClick={onClose} size="small"><CloseRounded /></IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          {error && <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>}
          <TextField label="Invite Code" fullWidth value={code}
            onChange={(e) => setCode(e.target.value.toUpperCase())}
            inputProps={{ maxLength: 8, style: { letterSpacing: 4, fontWeight: 700 } }}
            placeholder="XXXXXXXX"
          />
          <Button variant="contained" fullWidth size="large"
            onClick={handleJoin} disabled={loading}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            {loading ? "Joining..." : "Join"}
          </Button>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

const ClassroomPage = () => {
  const dispatch   = useDispatch<AppDispatch>();
  const classrooms = useSelector(selectClassrooms);
  const loading    = useSelector(selectClassroomLoading);
  const error      = useSelector(selectClassroomError);

  const [createOpen, setCreateOpen] = useState(false);
  const [joinOpen,   setJoinOpen]   = useState(false);

  useEffect(() => {
    dispatch(fetchMyClassrooms());
    dispatch(fetchMyPaths());
  }, [dispatch]);

  return (
    <Box>
      <Stack direction="row" alignItems="center" justifyContent="space-between" mb={4}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Classrooms</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Collaborate, learn, and grow with your peers.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1.5}>
          <Button variant="outlined" startIcon={<LoginRounded />}
            onClick={() => setJoinOpen(true)}
            sx={{ borderRadius: 2 }}>
            Join
          </Button>
          <Button variant="contained" startIcon={<AddRounded />}
            onClick={() => setCreateOpen(true)}
            sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
            New Classroom
          </Button>
        </Stack>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>}

      {loading ? (
        <Grid container spacing={3}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Grid item xs={12} sm={6} lg={4} key={i}>
              <Skeleton variant="rounded" height={220} sx={{ borderRadius: 4 }} />
            </Grid>
          ))}
        </Grid>
      ) : classrooms.length === 0 ? (
        <EmptyState
          title="No classrooms yet."
          description="Create a new classroom or join one with an invite code."
          icon={<GroupsRounded sx={{ fontSize: 48 }} />}
        />
      ) : (
        <Grid container spacing={3}>
          {classrooms.map((c) => (
            <Grid item xs={12} sm={6} lg={4} key={c.id}>
              <ClassroomCard classroom={c} />
            </Grid>
          ))}
        </Grid>
      )}

      <CreateClassroomModal open={createOpen} onClose={() => setCreateOpen(false)} />
      <JoinClassroomModal   open={joinOpen}   onClose={() => setJoinOpen(false)} />
    </Box>
  );
};

export default ClassroomPage;
