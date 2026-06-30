import { useEffect, useState, useCallback } from "react";
import {
  Alert, Avatar, Box, Button, Chip, IconButton, MenuItem, Paper, Select,
  Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Typography, CircularProgress,
} from "@mui/material";
import { DeleteRounded, SearchRounded } from "@mui/icons-material";
import { adminService } from "../../services/adminService";
import { AdminUser } from "../../types/admin.types";

const ROLES = ["Student", "Instructor", "Admin"];

const AdminUsersPage = () => {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [savingId, setSavingId] = useState<string | null>(null);

  const load = useCallback(async (q?: string) => {
    setLoading(true);
    try {
      const data = await adminService.getUsers(q || undefined);
      setUsers(data);
    } catch { setError("Failed to load users."); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const handleRoleChange = async (userId: string, role: string) => {
    setSavingId(userId);
    try {
      await adminService.updateRole(userId, role);
      await load(search);
    } catch { setError("Failed to update role."); }
    finally { setSavingId(null); }
  };

  const handleToggleStatus = async (user: AdminUser) => {
    setSavingId(user.userId);
    try {
      await adminService.toggleStatus(user.userId, !user.isActive);
      await load(search);
    } catch { setError("Failed to toggle status."); }
    finally { setSavingId(null); }
  };

  const handleDelete = async (userId: string, name: string) => {
    if (!window.confirm(`Delete user "${name}"? This cannot be undone.`)) return;
    try {
      await adminService.deleteUser(userId);
      await load(search);
    } catch { setError("Failed to delete user."); }
  };

  const handleSearch = () => { load(search); };

  if (loading) return <Box sx={{ p: 4, textAlign: "center" }}><CircularProgress /></Box>;

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" fontWeight={700} mb={3}>User Management</Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Stack direction="row" spacing={1} mb={3}>
        <TextField size="small" placeholder="Search by name or email..." value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSearch()}
          sx={{ minWidth: 300 }} />
        <Button variant="outlined" startIcon={<SearchRounded />} onClick={handleSearch}>Search</Button>
      </Stack>

      <TableContainer component={Paper} sx={{ borderRadius: 4 }}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>User</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Email</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Role</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Paths Created</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Modules Done</TableCell>
              <TableCell sx={{ fontWeight: 700 }} align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map((u) => (
              <TableRow key={u.userId} hover>
                <TableCell>
                  <Stack direction="row" alignItems="center" spacing={1.5}>
                    <Avatar sx={{ width: 32, height: 32, fontSize: "0.8rem", fontWeight: 700 }}>
                      {u.fullName.split(" ").map(n => n[0]).join("")}
                    </Avatar>
                    <Typography fontWeight={600}>{u.fullName}</Typography>
                  </Stack>
                </TableCell>
                <TableCell>{u.email}</TableCell>
                <TableCell>
                  <Select size="small" value={u.roles[0] || "Student"}
                    disabled={savingId === u.userId}
                    onChange={(e) => handleRoleChange(u.userId, e.target.value)}
                    sx={{ minWidth: 110, fontSize: "0.85rem" }}>
                    {ROLES.map(r => <MenuItem key={r} value={r}>{r}</MenuItem>)}
                  </Select>
                </TableCell>
                <TableCell>
                  <Chip label={u.isActive ? "Active" : "Inactive"}
                    color={u.isActive ? "success" : "default"} size="small"
                    onClick={() => handleToggleStatus(u)}
                    sx={{ cursor: "pointer" }} />
                </TableCell>
                <TableCell>{u.totalPathsCreated}</TableCell>
                <TableCell>{u.totalModulesCompleted}</TableCell>
                <TableCell align="right">
                  <IconButton onClick={() => handleDelete(u.userId, u.fullName)}
                    size="small" color="error" title="Delete User">
                    <DeleteRounded />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};

export default AdminUsersPage;
