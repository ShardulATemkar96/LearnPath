import { useEffect, useState } from "react";
import {
  Alert, Avatar, Box, Button, Chip, CircularProgress, Dialog,
  DialogActions, DialogContent, DialogTitle, Divider, IconButton, MenuItem, Select,
  Stack, TextField, Tooltip, Typography,
} from "@mui/material";
import {
  CloseRounded, DeleteRounded, ShieldRounded,
  BlockRounded, CheckCircleRounded, ReportRounded,
} from "@mui/icons-material";
import { adminService } from "../../../services/adminService";
import { AdminUser } from "../../../types/admin.types";
import { dateUtils } from "../../../utils/dateUtils";
import { useApiError } from "../../../hooks/useApiError";
import ConfirmDialog from "../../common/ConfirmDialog/ConfirmDialog";

const ROLES = ["Student", "Instructor", "Admin"];

interface UserDetailsModalProps {
  open: boolean;
  userId: string | null;
  onClose: () => void;
  onChanged?: () => void;
}

const statusChip = (user: AdminUser) => {
  const label = user.status;
  const color =
    label === "Active" ? "success"
    : label === "Invalid" ? "warning"
    : label === "Inactive" ? "default"
    : "error";
  return (
    <Chip
      label={label}
      size="small"
      color={color}
      variant={label === "Deleted" || label === "Invalid" ? "filled" : "outlined"}
      sx={{ fontWeight: 600 }}
    />
  );
};

const statItem = (label: string, value: number) => (
  <Box
    sx={{
      flex: 1,
      minWidth: 110,
      p: 1.5,
      borderRadius: 2,
      bgcolor: "rgba(108,99,255,0.06)",
      textAlign: "center",
    }}
  >
    <Typography variant="h6" fontWeight={700} color="primary.main">
      {value}
    </Typography>
    <Typography variant="caption" color="text.secondary">
      {label}
    </Typography>
  </Box>
);

const infoRow = (label: string, value: string) => (
  <Box>
    <Typography variant="caption" color="text.secondary" fontWeight={600}>
      {label}
    </Typography>
    <Typography variant="body2" fontWeight={500} sx={{ wordBreak: "break-word" }}>
      {value || "—"}
    </Typography>
  </Box>
);

const UserDetailsModal = ({
  open, userId, onClose, onChanged,
}: UserDetailsModalProps) => {
  const { error, handleError, clearError } = useApiError();
  const [user, setUser] = useState<AdminUser | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [invalidOpen, setInvalidOpen] = useState(false);
  const [invalidReason, setInvalidReason] = useState("");
  const [success, setSuccess] = useState("");

  useEffect(() => {
    if (!open || !userId) return;
    setUser(null);
    setSuccess("");
    clearError();
    setLoading(true);
    adminService.getUser(userId)
      .then(setUser)
      .catch(handleError)
      .finally(() => setLoading(false));
  }, [open, userId, clearError]);

  const isDeleted = user?.status === "Deleted";
  const isSuperAdmin = !!user?.isSuperAdmin;

  const runAction = async (action: () => Promise<AdminUser>) => {
    if (!user) return;
    setSaving(true);
    setSuccess("");
    clearError();
    try {
      const updated = await action();
      setUser(updated);
      onChanged?.();
    } catch (err) {
      handleError(err);
    } finally {
      setSaving(false);
    }
  };

  const handleActivate = () =>
    runAction(() => adminService.activateUser(user!.userId));

  const handleDeactivate = () =>
    runAction(() => adminService.deactivateUser(user!.userId));

  const handleDelete = async () => {
    setDeleteOpen(false);
    if (!user) return;
    setSaving(true);
    setSuccess("");
    clearError();
    try {
      await adminService.deleteUser(user.userId);
      setUser(null);
      setSuccess("User deleted.");
      onChanged?.();
      onClose();
    } catch (err) {
      handleError(err);
      setSaving(false);
    }
  };

  const handleMarkInvalid = async () => {
    if (!user || !invalidReason.trim()) return;
    setSaving(true);
    setSuccess("");
    clearError();
    try {
      const updated = await adminService.markInvalid(user.userId, invalidReason.trim());
      setUser(updated);
      setInvalidOpen(false);
      setInvalidReason("");
      onChanged?.();
    } catch (err) {
      handleError(err);
    } finally {
      setSaving(false);
    }
  };

  const handleRoleChange = async (role: string) => {
    if (!user || role === user.roles[0]) return;
    setSaving(true);
    setSuccess("");
    clearError();
    try {
      const updated = await adminService.updateRole(user.userId, role);
      setUser(updated);
      onChanged?.();
    } catch (err) {
      handleError(err);
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Dialog
        open={open}
        onClose={onClose}
        maxWidth="sm"
        fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}
      >
        <DialogTitle sx={{ pb: 1 }}>
          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6" fontWeight={700}>User Details</Typography>
            <IconButton onClick={onClose} size="small">
              <CloseRounded />
            </IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent dividers>
          {loading || !user ? (
            <Box sx={{ py: 6, textAlign: "center" }}>
              <CircularProgress size={28} />
            </Box>
          ) : (
            <Stack spacing={2.5}>
              {(error || success) && (
                <Alert severity={error ? "error" : "success"} sx={{ borderRadius: 2 }}>
                  {error || success}
                </Alert>
              )}

              <Stack direction="row" alignItems="center" spacing={2}>
                <Avatar
                  src={user.avatarUrl || undefined}
                  sx={{ width: 56, height: 56, fontSize: "1.1rem", fontWeight: 700 }}
                >
                  {user.fullName.split(" ").map((n) => n[0]).join("")}
                </Avatar>
                <Box flex={1}>
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <Typography variant="h6" fontWeight={700}>
                      {user.fullName}
                    </Typography>
                    {isSuperAdmin && (
                      <Tooltip title="Super Admin">
                        <ShieldRounded sx={{ color: "warning.main", fontSize: 18 }} />
                      </Tooltip>
                    )}
                  </Stack>
                  <Stack direction="row" alignItems="center" spacing={1} mt={0.5}>
                    <Chip
                      label={user.roles[0] || "Student"}
                      size="small"
                      variant="outlined"
                      sx={{ fontWeight: 600 }}
                    />
                    {statusChip(user)}
                  </Stack>
                </Box>
              </Stack>

              <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                {statItem("Modules Done", user.totalModulesCompleted)}
                {statItem("Quiz Attempts", user.totalQuizAttempts)}
                {statItem("Certificates", user.totalCertificates)}
                {statItem("Classrooms", user.totalClassroomsJoined)}
                {statItem("Paths Created", user.totalPathsCreated)}
              </Box>

              <Divider />

              <Stack spacing={1.5}>
                {infoRow("Username", user.userName)}
                {infoRow("Email", user.email)}
                {infoRow("Phone", user.phoneNumber ?? "")}
                {infoRow("Bio", user.bio ?? "")}
                {user.status === "Invalid" && infoRow("Invalid Reason", user.invalidReason ?? "")}
                <Stack direction="row" flexWrap="wrap" gap={1.5}>
                  <Box sx={{ flex: 1, minWidth: 160 }}>
                    {infoRow("Joined", dateUtils.format(user.createdAt))}
                  </Box>
                  <Box sx={{ flex: 1, minWidth: 160 }}>
                    {infoRow("Last Login", user.lastLoginAt
                      ? dateUtils.format(user.lastLoginAt)
                      : "Never")}
                  </Box>
                </Stack>
              </Stack>

              <Divider />

              <Stack spacing={1.5}>
                <Stack direction="row" alignItems="center"
                  justifyContent="space-between" flexWrap="wrap" gap={1}>
                  <Typography variant="body2" fontWeight={600}>Role</Typography>
                  <Select
                    size="small"
                    value={user.roles[0] || "Student"}
                    disabled={saving || isDeleted}
                    onChange={(e) => handleRoleChange(e.target.value)}
                    sx={{ minWidth: 150, fontSize: "0.85rem" }}
                  >
                    {ROLES.map((r) => (
                      <MenuItem key={r} value={r}>{r}</MenuItem>
                    ))}
                  </Select>
                </Stack>

                <Stack direction="row" flexWrap="wrap" gap={1}>
                  {user.status !== "Active" ? (
                    <Button
                      variant="contained"
                      color="success"
                      size="small"
                      disabled={saving || isDeleted}
                      onClick={handleActivate}
                      startIcon={<CheckCircleRounded />}
                      sx={{ borderRadius: 2 }}
                    >
                      Activate
                    </Button>
                  ) : (
                    <Button
                      variant="outlined"
                      color="warning"
                      size="small"
                      disabled={saving}
                      onClick={handleDeactivate}
                      startIcon={<BlockRounded />}
                      sx={{ borderRadius: 2 }}
                    >
                      Deactivate
                    </Button>
                  )}
                  <Tooltip title={isSuperAdmin
                    ? "The Super Admin account cannot be deleted."
                    : isDeleted ? "This user is already deleted." : "Soft delete this user"}
                  >
                    <span>
                      <Button
                        variant="outlined"
                        color="error"
                        size="small"
                        disabled={saving || isSuperAdmin || isDeleted}
                        onClick={() => setDeleteOpen(true)}
                        startIcon={<DeleteRounded />}
                        sx={{ borderRadius: 2 }}
                      >
                        Delete
                      </Button>
                    </span>
                  </Tooltip>
                  {!isSuperAdmin && !isDeleted && user.status !== "Invalid" && (
                    <Button
                      variant="outlined"
                      color="warning"
                      size="small"
                      disabled={saving}
                      onClick={() => setInvalidOpen(true)}
                      startIcon={<ReportRounded />}
                      sx={{ borderRadius: 2 }}
                    >
                      Mark Invalid
                    </Button>
                  )}
                </Stack>
              </Stack>
            </Stack>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={deleteOpen}
        title="Delete User"
        message={`Soft delete "${user?.fullName}"? Their account will be deactivated and their identity anonymized. This cannot be undone.`}
        confirmLabel="Delete User"
        onConfirm={handleDelete}
        onCancel={() => setDeleteOpen(false)}
      />

      <Dialog
        open={invalidOpen}
        onClose={() => setInvalidOpen(false)}
        maxWidth="xs"
        fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}
      >
        <DialogTitle sx={{ pb: 1 }}>
          <Typography variant="h6" fontWeight={700}>Mark Invalid</Typography>
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" mb={1.5}>
            Mark "{user?.fullName}" as invalid? They will be temporarily blocked from
            the platform until restored by an administrator. A reason is required.
          </Typography>
          <TextField
            fullWidth
            multiline
            minRows={2}
            label="Reason"
            placeholder="e.g. Repeatedly submitted plagiarized work."
            value={invalidReason}
            onChange={(e) => setInvalidReason(e.target.value)}
            error={saving && !invalidReason.trim()}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button size="small" onClick={() => setInvalidOpen(false)}>Cancel</Button>
          <Button
            size="small"
            variant="contained"
            color="warning"
            disabled={saving || !invalidReason.trim()}
            onClick={handleMarkInvalid}
            startIcon={<ReportRounded />}
          >
            Mark Invalid
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default UserDetailsModal;
