import { useEffect, useState, useCallback } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert, Box, Button, Chip, Dialog, DialogContent, DialogTitle, Divider, IconButton,
  Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead,
  TableRow, TextField, Typography, CircularProgress, MenuItem, Select, InputLabel, FormControl,
  Switch,
} from "@mui/material";
import {
  AddRounded, CloseRounded, DeleteRounded, EditRounded, ArrowBackRounded,
  ArrowUpwardRounded, ArrowDownwardRounded, ArchiveRounded, UnarchiveRounded,
  PublishRounded, QuizRounded, UndoRounded,
} from "@mui/icons-material";
import { pathService } from "../../services/pathService";
import { LearningPathDetail, Module, CreateModuleRequest } from "../../types/path.types";
import { quizService } from "../../services/quizService";
import { ROUTES } from "../../constants/routes";

const CONTENT_TYPES = ["video", "article", "quiz", "code", "document"];
const DIFFICULTY_LABELS = ["Beginner", "Intermediate", "Advanced"];

const emptyForm = (nextOrder: number): CreateModuleRequest => ({
  title: "", description: "", contentType: "article", contentUrl: "", order: nextOrder,
  difficulty: 0, estimatedDurationMinutes: undefined, notesHtml: "", pdfUrl: "",
  thumbnailUrl: "", isDraft: true,
  resources: [], objectives: [], tags: [],
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
  const [search, setSearch] = useState("");
  const [showArchived, setShowArchived] = useState(false);

  const filteredModules = modules.filter(m => {
    if (!showArchived && m.isArchived) return false;
    if (!search.trim()) return true;
    const q = search.toLowerCase();
    return m.title.toLowerCase().includes(q) || m.description.toLowerCase().includes(q);
  });

  const loadPath = useCallback(async () => {
    setLoading(true);
    try {
      const detail = await pathService.getById(pid, true);
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
      difficulty: mod.difficulty,
      estimatedDurationMinutes: mod.estimatedDurationMinutes,
      notesHtml: mod.notesHtml || "",
      pdfUrl: mod.pdfUrl || "",
      thumbnailUrl: mod.thumbnailUrl || "",
      isDraft: true,
      resources: mod.resources.map(r => ({ type: r.type, title: r.title, url: r.url, orderIndex: r.orderIndex })),
      objectives: mod.objectives.map(o => ({ objectiveText: o.objectiveText, orderIndex: o.orderIndex })),
      tags: [...mod.tags],
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

      <Stack direction="row" spacing={2} mb={2}>
        <TextField size="small" placeholder="Search modules..." value={search}
          onChange={(e) => setSearch(e.target.value)} sx={{ flex: 1, maxWidth: 400 }} />
        <Button
          variant={showArchived ? "contained" : "outlined"} size="small"
          color={showArchived ? "warning" : "inherit"}
          startIcon={<ArchiveRounded />}
          onClick={() => setShowArchived(s => !s)}>
          {showArchived ? "Hide Archived" : "Show Archived"}
        </Button>
      </Stack>

      {filteredModules.length === 0 ? (
        <Paper sx={{ p: 4, textAlign: "center", borderRadius: 4 }}>
          <Typography color="text.secondary">No modules yet. Click "Add Module" to create one.</Typography>
        </Paper>
      ) : (
        <TableContainer component={Paper} sx={{ borderRadius: 4 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700, width: 50 }}>#</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Title</TableCell>
                <TableCell sx={{ fontWeight: 700, width: 90 }}>Type</TableCell>
                <TableCell sx={{ fontWeight: 700, width: 100 }}>Status</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Description</TableCell>
                <TableCell sx={{ fontWeight: 700, minWidth: 220 }} align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredModules.map((mod, index) => (
                <TableRow key={mod.id} hover sx={{ opacity: mod.isArchived ? 0.6 : 1 }}>
                  <TableCell>{mod.order}</TableCell>
                  <TableCell><Typography fontWeight={600}>{mod.title}</Typography></TableCell>
                  <TableCell><Chip label={mod.contentType} size="small" variant="outlined" /></TableCell>
                  <TableCell>
                    {mod.isArchived ? (
                      <Chip label="Archived" size="small" color="warning" variant="outlined" />
                    ) : mod.isPublished ? (
                      <Chip label="Published" size="small" color="success" variant="outlined" />
                    ) : (
                      <Chip label="Draft" size="small" color="default" variant="outlined" />
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary" noWrap sx={{ maxWidth: 200 }}>
                      {mod.description}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    <IconButton onClick={() => pathService.reorderModule(pid, mod.id, true).then(loadPath)}
                      size="small" disabled={index === 0} title="Move Up">
                      <ArrowUpwardRounded fontSize="small" />
                    </IconButton>
                    <IconButton onClick={() => pathService.reorderModule(pid, mod.id, false).then(loadPath)}
                      size="small" disabled={index === filteredModules.length - 1} title="Move Down">
                      <ArrowDownwardRounded fontSize="small" />
                    </IconButton>
                    <IconButton onClick={() => openEdit(mod)} size="small" color="primary" title="Edit">
                      <EditRounded />
                    </IconButton>
                    {mod.isPublished ? (
                      <IconButton onClick={() => pathService.unpublishModule(pid, mod.id).then(loadPath)}
                        size="small" color="default" title="Unpublish">
                        <UndoRounded fontSize="small" />
                      </IconButton>
                    ) : !mod.isArchived ? (
                      <IconButton onClick={() => pathService.publishModule(pid, mod.id).then(loadPath)}
                        size="small" color="success" title="Publish">
                        <PublishRounded fontSize="small" />
                      </IconButton>
                    ) : null}
                    {mod.isArchived ? (
                      <IconButton onClick={() => pathService.unarchiveModule(pid, mod.id).then(loadPath)}
                        size="small" color="warning" title="Unarchive">
                        <UnarchiveRounded fontSize="small" />
                      </IconButton>
                    ) : (
                      <IconButton onClick={() => pathService.archiveModule(pid, mod.id).then(loadPath)}
                        size="small" color="inherit" title="Archive">
                        <ArchiveRounded fontSize="small" />
                      </IconButton>
                    )}
                      <IconButton onClick={() => navigate(ROUTES.ADMIN_QUIZ_EDITOR.replace(":pathId", String(pid)).replace(":moduleId", String(mod.id)))}
                        size="small" color="info" title="Manage Quiz">
                        <QuizRounded fontSize="small" />
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

            <FormControl fullWidth>
              <InputLabel>Difficulty</InputLabel>
              <Select value={form.difficulty} label="Difficulty"
                onChange={(e) => setForm(p => ({ ...p, difficulty: Number(e.target.value) }))}>
                <MenuItem value={0}>Beginner</MenuItem>
                <MenuItem value={1}>Intermediate</MenuItem>
                <MenuItem value={2}>Advanced</MenuItem>
              </Select>
            </FormControl>

            <TextField label="Estimated Duration (minutes)" type="number" fullWidth
              value={form.estimatedDurationMinutes ?? ""}
              onChange={(e) => setForm(p => ({ ...p, estimatedDurationMinutes: e.target.value ? parseInt(e.target.value) : undefined }))} />

            <TextField label="Thumbnail URL" fullWidth value={form.thumbnailUrl}
              onChange={(e) => setForm(p => ({ ...p, thumbnailUrl: e.target.value }))} />

            <TextField label="PDF URL" fullWidth value={form.pdfUrl}
              onChange={(e) => setForm(p => ({ ...p, pdfUrl: e.target.value }))} />

            <Stack direction="row" alignItems="center">
              <Switch checked={form.isDraft}
                onChange={(e) => setForm(p => ({ ...p, isDraft: e.target.checked }))} />
              <Typography variant="body2">Draft</Typography>
            </Stack>

            <TextField label="Tags (comma-separated)" fullWidth
              value={form.tags.join(", ")}
              onChange={(e) => setForm(p => ({ ...p, tags: e.target.value.split(",").map(t => t.trim()).filter(Boolean) }))} />

            <Divider />

            <Typography variant="subtitle2" fontWeight={600}>Notes</Typography>
            <TextField label="Notes (HTML supported)" fullWidth multiline minRows={4} maxRows={12}
              value={form.notesHtml || ""}
              onChange={(e) => setForm(p => ({ ...p, notesHtml: e.target.value }))} />

            <Divider />

            <Typography variant="subtitle2" fontWeight={600}>Resources</Typography>
            {form.resources.map((r, i) => (
              <Stack key={i} direction="row" spacing={1} alignItems="center">
                <TextField size="small" label="Type" value={r.type}
                  onChange={(e) => {
                    const updated = [...form.resources];
                    updated[i] = { ...updated[i], type: e.target.value };
                    setForm(p => ({ ...p, resources: updated }));
                  }} sx={{ width: 100 }} />
                <TextField size="small" label="Title" value={r.title}
                  onChange={(e) => {
                    const updated = [...form.resources];
                    updated[i] = { ...updated[i], title: e.target.value };
                    setForm(p => ({ ...p, resources: updated }));
                  }} sx={{ flex: 1 }} />
                <TextField size="small" label="URL" value={r.url}
                  onChange={(e) => {
                    const updated = [...form.resources];
                    updated[i] = { ...updated[i], url: e.target.value };
                    setForm(p => ({ ...p, resources: updated }));
                  }} sx={{ flex: 1 }} />
                <IconButton size="small" color="error" onClick={() =>
                  setForm(p => ({ ...p, resources: p.resources.filter((_, j) => j !== i) }))}>
                  <DeleteRounded fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button variant="outlined" size="small" startIcon={<AddRounded />}
              onClick={() => setForm(p => ({ ...p, resources: [...p.resources, { type: "video", title: "", url: "", orderIndex: p.resources.length }] }))}>
              Add Resource
            </Button>

            <Divider />

            <Typography variant="subtitle2" fontWeight={600}>Objectives</Typography>
            {form.objectives.map((o, i) => (
              <Stack key={i} direction="row" spacing={1} alignItems="center">
                <TextField size="small" label="Objective" value={o.objectiveText} fullWidth
                  onChange={(e) => {
                    const updated = [...form.objectives];
                    updated[i] = { ...updated[i], objectiveText: e.target.value };
                    setForm(p => ({ ...p, objectives: updated }));
                  }} />
                <IconButton size="small" color="error" onClick={() =>
                  setForm(p => ({ ...p, objectives: p.objectives.filter((_, j) => j !== i) }))}>
                  <DeleteRounded fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <Button variant="outlined" size="small" startIcon={<AddRounded />}
              onClick={() => setForm(p => ({ ...p, objectives: [...p.objectives, { objectiveText: "", orderIndex: p.objectives.length }] }))}>
              Add Objective
            </Button>

            <Divider />

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
