import { useEffect, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, IconButton, Paper, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Typography, CircularProgress,
  Dialog, DialogContent, DialogTitle, TextField, FormControlLabel, Switch,
} from "@mui/material";
import {
  AddRounded, CloseRounded, DeleteRounded, EditRounded,
  LibraryAddRounded,
} from "@mui/icons-material";
import { adminService } from "../../services/adminService";
import { pathService } from "../../services/pathService";
import { AdminPath } from "../../types/admin.types";
import { useDispatch } from "react-redux";
import { AppDispatch } from "../../redux/store";
import { fetchMyPaths } from "../../redux/slices/pathSlice";

const AdminPathsPage = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch<AppDispatch>();
  const [paths, setPaths] = useState<AdminPath[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [editModal, setEditModal] = useState(false);
  const [editingPath, setEditingPath] = useState<AdminPath | null>(null);
  const [form, setForm] = useState({ title: "", description: "", thumbnailUrl: "", isPublic: false, isPublished: false });
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await adminService.getAllPaths();
      setPaths(data);
    } catch { setError("Failed to load paths."); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openEdit = (p: AdminPath) => {
    setEditingPath(p);
    setForm({ title: p.title, description: p.description, thumbnailUrl: p.thumbnailUrl || "", isPublic: p.isPublic, isPublished: p.isPublished });
    setEditModal(true);
  };

  const handleSave = async () => {
    if (!editingPath) return;
    setSaving(true);
    try {
      await pathService.update(editingPath.id, form);
      dispatch(fetchMyPaths());
      await load();
      setEditModal(false);
    } catch { setError("Failed to update path."); }
    finally { setSaving(false); }
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm("Delete this learning path? This cannot be undone.")) return;
    try {
      await pathService.delete(id);
      dispatch(fetchMyPaths());
      await load();
    } catch { setError("Failed to delete path."); }
  };

  if (loading) return <Box sx={{ p: 4, textAlign: "center" }}><CircularProgress /></Box>;

  return (
    <Box sx={{ p: 3 }}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" mb={3}>
        <Typography variant="h5" fontWeight={700}>Learning Paths</Typography>
        <Button variant="contained" startIcon={<AddRounded />}
          onClick={() => navigate("/paths")}
          sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
          Create New
        </Button>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {paths.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: "center", borderRadius: 4 }}>
          <Typography color="text.secondary">No learning paths found.</Typography>
        </Paper>
      ) : (
        <TableContainer component={Paper} sx={{ borderRadius: 4 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Title</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Modules</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Created By</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Updated</TableCell>
                <TableCell sx={{ fontWeight: 700 }} align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {paths.map((p) => (
                <TableRow key={p.id} hover>
                  <TableCell>
                    <Typography fontWeight={600}>{p.title}</Typography>
                    <Typography variant="body2" color="text.secondary" noWrap sx={{ maxWidth: 300 }}>
                      {p.description}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Stack direction="row" spacing={0.5}>
                      {p.isPublished && <Chip label="Published" color="success" size="small" />}
                      {p.isPublic && <Chip label="Public" color="primary" size="small" variant="outlined" />}
                      {!p.isPublished && <Chip label="Draft" size="small" />}
                    </Stack>
                  </TableCell>
                  <TableCell>{p.totalModules}</TableCell>
                  <TableCell>{p.createdByName}</TableCell>
                  <TableCell>{new Date(p.updatedAt).toLocaleDateString()}</TableCell>
                  <TableCell align="right">
                    <IconButton onClick={() => openEdit(p)} size="small" color="primary" title="Edit Path">
                      <EditRounded />
                    </IconButton>
                    <IconButton onClick={() => navigate(`/admin/paths/${p.id}/modules`)} size="small" color="info" title="Manage Modules">
                      <LibraryAddRounded />
                    </IconButton>
                    <IconButton onClick={() => handleDelete(p.id)} size="small" color="error" title="Delete">
                      <DeleteRounded />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={editModal} onClose={() => setEditModal(false)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6" fontWeight={700}>Edit Learning Path</Typography>
            <IconButton onClick={() => setEditModal(false)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField label="Title" fullWidth value={form.title}
              onChange={(e) => setForm(p => ({ ...p, title: e.target.value }))} />
            <TextField label="Description" fullWidth multiline rows={3} value={form.description}
              onChange={(e) => setForm(p => ({ ...p, description: e.target.value }))} />
            <TextField label="Thumbnail URL" fullWidth value={form.thumbnailUrl}
              onChange={(e) => setForm(p => ({ ...p, thumbnailUrl: e.target.value }))} />
            <FormControlLabel control={<Switch checked={form.isPublic}
              onChange={(e) => setForm(p => ({ ...p, isPublic: e.target.checked }))} />}
              label="Public" />
            <FormControlLabel control={<Switch checked={form.isPublished}
              onChange={(e) => setForm(p => ({ ...p, isPublished: e.target.checked }))} />}
              label="Published" />
            <Button variant="contained" fullWidth size="large" onClick={handleSave} disabled={saving}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              {saving ? <CircularProgress size={22} sx={{ color: "#fff" }} /> : "Save Changes"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default AdminPathsPage;
