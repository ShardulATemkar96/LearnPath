import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogContent, DialogTitle, IconButton,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead,
  TableRow, TextField, Typography, CircularProgress, MenuItem, Select, InputLabel, FormControl,
} from "@mui/material";
import {
  AddRounded, CloseRounded, DeleteRounded, EditRounded, ArrowBackRounded,
} from "@mui/icons-material";
import { pathService } from "../../services/pathService";
import { LearningPathDetail, Module, CreateModuleRequest } from "../../types/path.types";

const CONTENT_TYPES = ["video", "article", "quiz", "code", "document"];

const emptyForm = (nextOrder: number): CreateModuleRequest => ({
  title: "", description: "", contentType: "article", contentUrl: "", order: nextOrder,
});

const AdminModuleEditorPage = () => {
  const { pathId } = useParams<{ pathId: string }>();
  const navigate = useNavigate();
  const pid = Number(pathId);

  const [pathDetail, setPathDetail] = useState<LearningPathDetail | null>(null);
  const [modules, setModules] = useState<Module[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [editingModule, setEditingModule] = useState<Module | null>(null);
  const [form, setForm] = useState<CreateModuleRequest>(emptyForm(1));
  const [saving, setSaving] = useState(false);

  const loadPath = useCallback(async () => {
    setLoading(true);
    try {
      const detail = await pathService.getById(pid);
      setPathDetail(detail);
      setModules(detail.modules.sort((a, b) => a.order - b.order));
    } catch { setError("Failed to load path."); }
    finally { setLoading(false); }
  }, [pid]);

  useEffect(() => { loadPath(); }, [loadPath]);

  const openAdd = () => {
    const nextOrder = modules.length > 0 ? Math.max(...modules.map(m => m.order)) + 1 : 1;
    setEditingModule(null);
    setForm(emptyForm(nextOrder));
    setModalOpen(true);
  };

  const openEdit = (mod: Module) => {
    setEditingModule(mod);
    setForm({
      title: mod.title,
      description: mod.description,
      contentType: mod.contentType,
      contentUrl: mod.contentUrl || "",
      order: mod.order,
    });
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!form.title.trim()) { setError("Title is required."); return; }
    setSaving(true);
    setError("");
    try {
      if (editingModule) {
        await pathService.updateModule(pid, editingModule.id, form);
      } else {
        await pathService.addModule(pid, form);
      }
      setModalOpen(false);
      await loadPath();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Failed to save module.");
    }
    finally { setSaving(false); }
  };

  const handleDelete = async (moduleId: number, title: string) => {
    if (!window.confirm(`Delete module "${title}"? This cannot be undone.`)) return;
    try {
      await pathService.deleteModule(pid, moduleId);
      await loadPath();
    } catch { setError("Failed to delete module."); }
  };

  if (loading) return <Box sx={{ p: 4, textAlign: "center" }}><CircularProgress /></Box>;
  if (!pathDetail) return <Box sx={{ p: 4 }}><Alert severity="error">Path not found.</Alert></Box>;

  return (
    <Box sx={{ p: 3 }}>
      <Stack direction="row" alignItems="center" spacing={1} mb={1}>
        <IconButton onClick={() => navigate("/admin/paths")}><ArrowBackRounded /></IconButton>
        <Box>
          <Typography variant="h5" fontWeight={700}>{pathDetail.title}</Typography>
          <Typography variant="body2" color="text.secondary">Manage modules for this learning path</Typography>
        </Box>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError("")}>{error}</Alert>}

      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2} mt={2}>
        <Typography variant="h6" fontWeight={600}>Modules ({modules.length})</Typography>
        <Button variant="contained" startIcon={<AddRounded />} onClick={openAdd}
          sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
          Add Module
        </Button>
      </Stack>

      {modules.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: "center", borderRadius: 4 }}>
          <Typography color="text.secondary">No modules yet. Click "Add Module" to create one.</Typography>
        </Paper>
      ) : (
        <TableContainer component={Paper} sx={{ borderRadius: 4 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700, width: 70 }}>Order</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Title</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Type</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Description</TableCell>
                <TableCell sx={{ fontWeight: 700 }} align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {modules.map((mod) => (
                <TableRow key={mod.id} hover>
                  <TableCell>{mod.order}</TableCell>
                  <TableCell><Typography fontWeight={600}>{mod.title}</Typography></TableCell>
                  <TableCell><Chip label={mod.contentType} size="small" variant="outlined" /></TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary" noWrap sx={{ maxWidth: 300 }}>
                      {mod.description}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <IconButton onClick={() => openEdit(mod)} size="small" color="primary" title="Edit">
                      <EditRounded />
                    </IconButton>
                    <IconButton onClick={() => handleDelete(mod.id, mod.title)} size="small" color="error" title="Delete">
                      <DeleteRounded />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={modalOpen} onClose={() => setModalOpen(false)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6" fontWeight={700}>
              {editingModule ? "Edit Module" : "Add Module"}
            </Typography>
            <IconButton onClick={() => setModalOpen(false)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField label="Title" fullWidth value={form.title}
              onChange={(e) => setForm(p => ({ ...p, title: e.target.value }))} />
            <TextField label="Description" fullWidth multiline rows={3} value={form.description}
              onChange={(e) => setForm(p => ({ ...p, description: e.target.value }))} />
            <FormControl fullWidth>
              <InputLabel>Content Type</InputLabel>
              <Select value={form.contentType} label="Content Type"
                onChange={(e) => setForm(p => ({ ...p, contentType: e.target.value }))}>
                {CONTENT_TYPES.map(ct => <MenuItem key={ct} value={ct}>{ct}</MenuItem>)}
              </Select>
            </FormControl>
            <TextField label="Content URL (optional)" fullWidth value={form.contentUrl || ""}
              onChange={(e) => setForm(p => ({ ...p, contentUrl: e.target.value }))} />
            <TextField label="Order" type="number" fullWidth value={form.order}
              onChange={(e) => setForm(p => ({ ...p, order: parseInt(e.target.value) || 0 }))} />
            <Button variant="contained" fullWidth size="large" onClick={handleSave} disabled={saving}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              {saving ? <CircularProgress size={22} sx={{ color: "#fff" }} /> :
                editingModule ? "Save Changes" : "Add Module"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default AdminModuleEditorPage;
