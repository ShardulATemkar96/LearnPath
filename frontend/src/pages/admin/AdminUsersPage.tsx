import { useEffect, useState, useCallback } from "react";
import {
  Alert, Avatar, Box, Button, Card, CardActionArea, CardContent, Chip,
  Grid, IconButton, Stack, TextField, Typography, CircularProgress, Tooltip,
} from "@mui/material";
import { DeleteRounded, SearchRounded, ShieldRounded } from "@mui/icons-material";
import { adminService } from "../../services/adminService";
import { AdminUser, UserStatus } from "../../types/admin.types";
import { useApiError } from "../../hooks/useApiError";
import UserDetailsModal from "../../components/admin/UserDetailsModal/UserDetailsModal";
import ConfirmDialog from "../../components/common/ConfirmDialog/ConfirmDialog";

const STATUS_COLORS: Record<UserStatus, "success" | "default" | "error" | "warning"> = {
  Active:   "success",
  Inactive: "default",
  Deleted:  "error",
  Invalid:  "warning",
};

const AdminUsersPage = () => {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AdminUser | null>(null);
  const [deleting, setDeleting] = useState(false);
  const { error, handleError, clearError } = useApiError();

  const load = useCallback(async (q?: string) => {
    setLoading(true);
    clearError();
    try {
      const data = await adminService.getUsers(q || undefined);
      setUsers(data);
    } catch (err) { handleError(err); }
    finally { setLoading(false); }
  }, [clearError, handleError]);

  useEffect(() => { load(); }, [load]);

  const handleSearch = () => { load(search); };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    clearError();
    try {
      await adminService.deleteUser(deleteTarget.userId);
      setDeleteTarget(null);
      await load(search);
    } catch (err) { handleError(err); }
    finally { setDeleting(false); }
  };

  if (loading) {
    return <Box sx={{ p: 4, textAlign: "center" }}><CircularProgress /></Box>;
  }

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" fontWeight={700} mb={3}>User Management</Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Stack direction="row" spacing={1} mb={3}>
        <TextField size="small" placeholder="Search by name or email..." value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSearch()}
          sx={{ minWidth: 300 }} />
        <Button variant="outlined" startIcon={<SearchRounded />} onClick={handleSearch}>
          Search
        </Button>
      </Stack>

      <Grid container spacing={2}>
        {users.map((u) => (
          <Grid item key={u.userId} xs={12} sm={6} md={4}>
            <Card sx={{ borderRadius: 3, height: "100%", position: "relative",
              boxShadow: "0 2px 12px rgba(0,0,0,0.06)" }}>
              <CardActionArea onClick={() => setSelectedId(u.userId)}>
                <CardContent>
                  <Stack direction="row" alignItems="center" spacing={1.5}>
                    <Avatar
                      src={u.avatarUrl || undefined}
                      sx={{ width: 44, height: 44, fontWeight: 700 }}
                    >
                      {u.fullName.split(" ").map((n) => n[0]).join("")}
                    </Avatar>
                    <Box flex={1} minWidth={0}>
                      <Stack direction="row" alignItems="center" spacing={0.5}>
                        <Typography fontWeight={700} noWrap>{u.fullName}</Typography>
                        {u.isSuperAdmin && (
                          <Tooltip title="Super Admin">
                            <ShieldRounded sx={{ color: "warning.main", fontSize: 16 }} />
                          </Tooltip>
                        )}
                      </Stack>
                      <Typography variant="caption" color="text.secondary" noWrap>
                        {u.email}
                      </Typography>
                    </Box>
                  </Stack>

                  <Stack direction="row" spacing={0.5} mt={1.5}>
                    <Chip label={u.roles[0] || "Student"} size="small"
                      variant="outlined" sx={{ fontWeight: 600 }} />
                    <Chip label={u.status} size="small" color={STATUS_COLORS[u.status]}
                      variant={u.status === "Deleted" ? "filled" : "outlined"}
                      sx={{ fontWeight: 600 }} />
                  </Stack>

                  <Stack direction="row" spacing={2} mt={1.5}>
                    <Box>
                      <Typography variant="h6" fontWeight={700} color="primary.main">
                        {u.totalPathsCreated}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Paths Created
                      </Typography>
                    </Box>
                    <Box>
                      <Typography variant="h6" fontWeight={700} color="primary.main">
                        {u.totalModulesCompleted}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Modules Done
                      </Typography>
                    </Box>
                  </Stack>
                </CardContent>
              </CardActionArea>

              {!u.isSuperAdmin && u.status !== "Deleted" && (
                <IconButton
                  size="small" color="error" title="Delete User"
                  onClick={() => setDeleteTarget(u)}
                  sx={{ position: "absolute", top: 8, right: 8 }}
                >
                  <DeleteRounded fontSize="small" />
                </IconButton>
              )}
            </Card>
          </Grid>
        ))}
      </Grid>

      <UserDetailsModal
        open={!!selectedId}
        userId={selectedId}
        onClose={() => setSelectedId(null)}
        onChanged={() => load(search)}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        title="Delete User"
        message={`Soft delete "${deleteTarget?.fullName}"? Their account will be deactivated and their identity anonymized. This cannot be undone.`}
        confirmLabel="Delete User"
        confirmLoading={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  );
};

export default AdminUsersPage;
