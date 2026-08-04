import { useCallback, useEffect, useRef, useState } from "react";
import {
  Alert, Avatar, Box, Button, Card, CardContent, Chip, CircularProgress,
  Dialog, DialogActions, DialogContent, DialogTitle, Divider, IconButton,
  MenuItem, Select, Skeleton, Snackbar, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, TextField, Tooltip, Typography,
} from "@mui/material";
import {
  CloseRounded, DeleteRounded, EditRounded, GroupsRounded,
  SearchRounded, SwapHorizRounded, VisibilityRounded,
} from "@mui/icons-material";
import { adminService } from "../../services/adminService";
import {
  AdminClassroom, AdminClassroomDetail, AdminPath, AdminUser,
} from "../../types/admin.types";
import { useApiError } from "../../hooks/useApiError";
import ConfirmDialog from "../../components/common/ConfirmDialog/ConfirmDialog";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";

const PAGE_SIZE = 20;

const formatDateTime = (iso: string) => {
  const date = new Date(iso.endsWith("Z") ? iso : `${iso}Z`);
  return date.toLocaleString("en-US", {
    year: "numeric", month: "short", day: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
};

const initials = (name: string) =>
  name.split(" ").map((n) => n[0]).join("").slice(0, 2).toUpperCase();

const DetailRow = ({ label, value }: { label: string; value: string }) => (
  <Box>
    <Typography variant="caption" color="text.secondary">{label}</Typography>
    <Typography variant="body2" fontWeight={600}>{value}</Typography>
  </Box>
);

const AdminClassroomsPage = () => {
  const [entries, setEntries] = useState<AdminClassroom[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pathFilter, setPathFilter] = useState<number | "">("");
  const [statusFilter, setStatusFilter] = useState<"All" | "Active" | "Archived">("All");

  const [paths, setPaths] = useState<AdminPath[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);

  const [viewTarget, setViewTarget] = useState<AdminClassroom | null>(null);
  const [detail, setDetail] = useState<AdminClassroomDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const [editTarget, setEditTarget] = useState<AdminClassroom | null>(null);
  const [editForm, setEditForm] = useState({ title: "", description: "", learningPathId: 0, trainerId: "" });
  const [saving, setSaving] = useState(false);

  const [reassignTarget, setReassignTarget] = useState<AdminClassroom | null>(null);
  const [reassignPathId, setReassignPathId] = useState(0);

  const [deleteTarget, setDeleteTarget] = useState<AdminClassroom | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const { error, handleError, clearError } = useApiError();
  const isFirstLoad = useRef(true);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    adminService.getAllPaths().then(setPaths).catch(() => {});
    adminService.getUsers().then(setUsers).catch(() => {});
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    clearError();
    try {
      const data = await adminService.getClassrooms({
        page,
        pageSize: PAGE_SIZE,
        search: debouncedSearch || undefined,
        learningPathId: pathFilter === "" ? undefined : Number(pathFilter),
        status: statusFilter === "All" ? undefined : statusFilter,
      });
      setEntries(data.entries);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err) { handleError(err); }
    finally { setLoading(false); isFirstLoad.current = false; }
  }, [page, debouncedSearch, pathFilter, statusFilter, clearError, handleError]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (!isFirstLoad.current) setPage(1);
  }, [debouncedSearch, pathFilter, statusFilter]);

  const openView = async (c: AdminClassroom) => {
    setViewTarget(c);
    setDetail(null);
    setDetailLoading(true);
    clearError();
    try {
      setDetail(await adminService.getClassroom(c.id));
    } catch (err) { handleError(err); }
    finally { setDetailLoading(false); }
  };

  const openEdit = (c: AdminClassroom) => {
    setEditTarget(c);
    setEditForm({
      title: c.title,
      description: c.description,
      learningPathId: c.learningPathId,
      trainerId: c.createdById,
    });
  };

  const handleSave = async () => {
    if (!editTarget) return;
    setSaving(true);
    clearError();
    try {
      await adminService.updateClassroom(editTarget.id, {
        title: editForm.title,
        description: editForm.description,
        learningPathId: editForm.learningPathId,
        trainerId: editForm.trainerId || undefined,
      });
      setEditTarget(null);
      setSuccessMsg("Classroom updated successfully.");
      await load();
    } catch (err) { handleError(err); }
    finally { setSaving(false); }
  };

  const openReassign = (c: AdminClassroom) => {
    setReassignTarget(c);
    setReassignPathId(c.learningPathId);
  };

  const handleReassign = async () => {
    if (!reassignTarget || !reassignPathId) return;
    setSaving(true);
    clearError();
    try {
      const updated = await adminService.reassignClassroomLearningPath(reassignTarget.id, reassignPathId);
      setReassignTarget(null);
      setSuccessMsg(`Classroom "${updated.title}" reassigned to "${updated.learningPathTitle}".`);
      await load();
    } catch (err) { handleError(err); }
    finally { setSaving(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    clearError();
    try {
      await adminService.deleteClassroom(deleteTarget.id);
      setDeleteTarget(null);
      setSuccessMsg(`Classroom "${deleteTarget.title}" deleted.`);
      await load();
    } catch (err) { handleError(err); }
    finally { setDeleting(false); }
  };

  const hasFilters = Boolean(debouncedSearch || pathFilter !== "" || statusFilter !== "All");

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Classrooms</Typography>
          <Typography variant="body2" color="text.secondary">
            View, edit, reassign, or delete classrooms.
          </Typography>
        </Box>
      </Stack>

      <Card sx={{ borderRadius: 3, mb: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}>
        <CardContent sx={{ p: 2.5, "&:last-child": { pb: 2.5 } }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              size="small"
              label="Search"
              placeholder="Name, invite code, path or trainer..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              sx={{ minWidth: 260, flexGrow: 1 }}
              InputProps={{ startAdornment: <SearchRounded sx={{ mr: 1, color: "text.disabled", fontSize: 18 }} /> }}
            />
            <Select
              size="small"
              value={pathFilter}
              onChange={(e) => setPathFilter(e.target.value as number | "")}
              displayEmpty
              sx={{ minWidth: 200 }}
            >
              <MenuItem value="">All Learning Paths</MenuItem>
              {paths.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.title}</MenuItem>
              ))}
            </Select>
            <Select
              size="small"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as "All" | "Active" | "Archived")}
              sx={{ minWidth: 140 }}
            >
              <MenuItem value="All">All Statuses</MenuItem>
              <MenuItem value="Active">Active</MenuItem>
              <MenuItem value="Archived">Archived</MenuItem>
            </Select>
          </Stack>
        </CardContent>
      </Card>

      {error && <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }} onClose={clearError}>{error}</Alert>}
      <Snackbar open={!!successMsg} autoHideDuration={3000} onClose={() => setSuccessMsg("")}
        message={successMsg} anchorOrigin={{ vertical: "bottom", horizontal: "center" }} />

      {loading ? (
        <Stack spacing={1.5}>
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} variant="rounded" height={52} sx={{ borderRadius: 2 }} />
          ))}
        </Stack>
      ) : entries.length === 0 ? (
        <EmptyState
          title={hasFilters ? "No matching classrooms." : "No classrooms yet."}
          description={
            hasFilters
              ? "Try a different search term or filter."
              : "Classrooms will appear here as users create them."
          }
          icon={<GroupsRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <>
          <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: "block" }}>
            {totalCount} {totalCount === 1 ? "classroom" : "classrooms"}
          </Typography>

          <TableContainer
            component={Card}
            sx={{ borderRadius: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}
          >
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 700 }}>Name</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Learning Path</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Trainer</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Members</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Created</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entries.map((c) => (
                  <TableRow key={c.id} hover>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      <Typography variant="body2" fontWeight={600}>{c.title}</Typography>
                      <Typography variant="caption" color="text.secondary">{c.inviteCode}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{c.learningPathTitle}</Typography>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" alignItems="center" spacing={1}>
                        <Avatar sx={{ width: 28, height: 28, fontSize: "0.7rem", fontWeight: 700 }}>
                          {initials(c.createdByName)}
                        </Avatar>
                        <Box>
                          <Typography variant="body2">{c.createdByName}</Typography>
                          <Typography variant="caption" color="text.secondary">{c.createdByEmail}</Typography>
                        </Box>
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Chip label={c.memberCount} size="small" variant="outlined" />
                    </TableCell>
                    <TableCell>
                      <Chip label={c.status} size="small"
                        sx={{ height: 20, fontSize: "0.68rem", fontWeight: 600, color: "#10B981", bgcolor: "#10B9811A" }} />
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      <Typography variant="caption">{formatDateTime(c.createdAt)}</Typography>
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                        <Tooltip title="View details">
                          <IconButton size="small" onClick={() => openView(c)}>
                            <VisibilityRounded fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Edit">
                          <IconButton size="small" color="primary" onClick={() => openEdit(c)}>
                            <EditRounded fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Reassign learning path">
                          <IconButton size="small" color="info" onClick={() => openReassign(c)}>
                            <SwapHorizRounded fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Delete classroom">
                          <IconButton size="small" color="error" onClick={() => setDeleteTarget(c)}>
                            <DeleteRounded fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </Stack>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

          <PaginationBar page={page} totalPages={totalPages} onChange={setPage} />
        </>
      )}

      {/* View detail */}
      <Dialog open={viewTarget !== null} onClose={() => setViewTarget(null)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Stack direction="row" spacing={1} alignItems="center">
              <GroupsRounded sx={{ color: "#6C63FF" }} />
              <Typography variant="h6" fontWeight={700}>{viewTarget?.title}</Typography>
            </Stack>
            <IconButton onClick={() => setViewTarget(null)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          {detailLoading ? (
            <Stack spacing={1.5}>
              {Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} variant="rounded" height={40} />
              ))}
            </Stack>
          ) : detail ? (
            <Stack spacing={2}>
              <DetailRow label="Description" value={detail.description || "—"} />
              <DetailRow label="Invite Code" value={detail.inviteCode} />
              <DetailRow label="Learning Path" value={detail.learningPathTitle} />
              <DetailRow label="Trainer" value={`${detail.createdByName} (${detail.createdByEmail})`} />
              <DetailRow label="Members" value={`${detail.memberCount}`} />
              <DetailRow label="Assignments" value={`${detail.assignmentCount}`} />
              <DetailRow label="Created At" value={formatDateTime(detail.createdAt)} />
              <Divider />
              <Typography variant="subtitle2" fontWeight={700}>Members</Typography>
              {detail.members.length === 0 ? (
                <Typography variant="body2" color="text.secondary">No members yet.</Typography>
              ) : (
                detail.members.map((m) => (
                  <Stack key={m.userId} direction="row" alignItems="center" spacing={1.5}>
                    <Avatar sx={{ width: 32, height: 32, fontSize: "0.7rem", fontWeight: 700 }}>
                      {initials(m.fullName)}
                    </Avatar>
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="body2" fontWeight={600}>{m.fullName}</Typography>
                      <Typography variant="caption" color="text.secondary">{m.email}</Typography>
                    </Box>
                    <Chip label={m.role} size="small" variant="outlined" />
                    <Chip label={m.status} size="small"
                      sx={{ height: 20, fontSize: "0.68rem", fontWeight: 600,
                        color: m.status === "Active" ? "#10B981" : "#EF4444",
                        bgcolor: m.status === "Active" ? "#10B9811A" : "#EF44441A" }} />
                  </Stack>
                ))
              )}
            </Stack>
          ) : null}
        </DialogContent>
      </Dialog>

      {/* Edit */}
      <Dialog open={editTarget !== null} onClose={() => setEditTarget(null)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6" fontWeight={700}>Edit Classroom</Typography>
            <IconButton onClick={() => setEditTarget(null)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField label="Title" fullWidth value={editForm.title}
              onChange={(e) => setEditForm(f => ({ ...f, title: e.target.value }))} />
            <TextField label="Description" fullWidth multiline rows={3} value={editForm.description}
              onChange={(e) => setEditForm(f => ({ ...f, description: e.target.value }))} />
            <Box>
              <Typography variant="caption" color="text.secondary">Learning Path</Typography>
              <Select fullWidth size="small" value={editForm.learningPathId}
                onChange={(e) => setEditForm(f => ({ ...f, learningPathId: Number(e.target.value) }))}>
                {paths.map((p) => (
                  <MenuItem key={p.id} value={p.id}>{p.title}</MenuItem>
                ))}
              </Select>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Trainer</Typography>
              <Select fullWidth size="small" value={editForm.trainerId}
                onChange={(e) => setEditForm(f => ({ ...f, trainerId: String(e.target.value) }))}>
                <MenuItem value="">Unchanged</MenuItem>
                {users.map((u) => (
                  <MenuItem key={u.userId} value={u.userId}>{u.fullName} ({u.email})</MenuItem>
                ))}
              </Select>
            </Box>
            <Button variant="contained" fullWidth size="large" onClick={handleSave} disabled={saving || !editForm.title.trim()}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              {saving ? <CircularProgress size={22} sx={{ color: "#fff" }} /> : "Save Changes"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>

      {/* Reassign */}
      <Dialog open={reassignTarget !== null} onClose={() => setReassignTarget(null)} maxWidth="xs" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6" fontWeight={700}>Reassign Learning Path</Typography>
            <IconButton onClick={() => setReassignTarget(null)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <Typography variant="body2" color="text.secondary">
              Reassign "{reassignTarget?.title}" from "{reassignTarget?.learningPathTitle}" to another learning path.
            </Typography>
            <Select fullWidth size="small" value={reassignPathId}
              onChange={(e) => setReassignPathId(Number(e.target.value))}>
              {paths.map((p) => (
                <MenuItem key={p.id} value={p.id}>{p.title}</MenuItem>
              ))}
            </Select>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3, gap: 1 }}>
          <Button onClick={() => setReassignTarget(null)} disabled={saving} sx={{ borderRadius: 2 }}>Cancel</Button>
          <Button variant="contained" color="primary" onClick={handleReassign} disabled={saving || !reassignPathId}
            sx={{ borderRadius: 2 }}>
            {saving ? <CircularProgress size={18} sx={{ color: "#fff" }} /> : "Reassign"}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Classroom"
        message={`Delete classroom "${deleteTarget?.title}" for "${deleteTarget?.learningPathTitle}"? All memberships will be removed. This cannot be undone.`}
        confirmLabel="Delete"
        confirmLoading={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  );
};

export default AdminClassroomsPage;
