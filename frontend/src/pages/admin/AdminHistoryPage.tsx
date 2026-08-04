import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert, Autocomplete, Box, Button, Card, CardContent, Chip,
  CircularProgress, MenuItem, Skeleton, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip,
  Typography,
} from "@mui/material";
import {
  HistoryRounded, LockRounded, LaunchRounded, BlockRounded,
} from "@mui/icons-material";
import { historyService } from "../../services/historyService";
import { adminService } from "../../services/adminService";
import {
  AdminHistoryEntry, HistoryActionOption,
} from "../../types/history.types";
import { AdminUser } from "../../types/admin.types";
import { ROUTES } from "../../constants/routes";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";

const PAGE_SIZE = 20;
const ROLES = ["Admin", "Instructor", "Student"];

const CATEGORY_COLORS: Record<string, string> = {
  Authentication: "#6C63FF",
  Account:        "#0EA5E9",
  Academic:       "#10B981",
  Assessment:     "#F59E0B",
  Community:      "#FF6584",
};

const categoryColor = (category: string) =>
  CATEGORY_COLORS[category] ?? "#6C63FF";

const formatDateTime = (iso: string) => {
  const date = new Date(iso.endsWith("Z") ? iso : `${iso}Z`);
  return date.toLocaleString("en-US", {
    year: "numeric", month: "short", day: "numeric",
    hour: "2-digit", minute: "2-digit",
  });
};

/** Maps an audited entity to the route that displays it, when one exists. */
const entityRoute = (entityType: string, entityId?: string | null): string | null => {
  if (!entityId) return null;

  switch (entityType) {
    case "LearningPath": return ROUTES.LEARNING_PATH_DETAIL.replace(":id", entityId);
    case "Classroom":    return ROUTES.CLASSROOM_DETAIL.replace(":id", entityId);
    case "Group":        return ROUTES.COMMUNITY_GROUP_DETAIL.replace(":groupId", entityId);
    case "Post":         return ROUTES.COMMUNITY_DETAIL.replace(":id", entityId);
    case "Quiz":         return ROUTES.ADMIN_QUIZZES;
    case "User":         return ROUTES.ADMIN_USERS;
    default:             return null;
  }
};

const EntityCell = ({ entry }: { entry: AdminHistoryEntry }) => {
  const navigate = useNavigate();

  if (!entry.entityType) return <Typography variant="caption">—</Typography>;

  const label = `${entry.entityType}${entry.entityId ? ` #${entry.entityId}` : ""}`;
  const route = entityRoute(entry.entityType, entry.entityId);

  // entityExists is null when the type has no destination at all.
  if (entry.entityExists === false) {
    return (
      <Tooltip title="Entity no longer exists.">
        <Stack direction="row" alignItems="center" spacing={0.5}>
          <BlockRounded sx={{ fontSize: 14, color: "text.disabled" }} />
          <Typography variant="caption" color="text.disabled">
            {label} — Entity no longer exists.
          </Typography>
        </Stack>
      </Tooltip>
    );
  }

  if (entry.entityExists && route) {
    return (
      <Button
        size="small"
        endIcon={<LaunchRounded sx={{ fontSize: 14 }} />}
        onClick={() => navigate(route)}
        sx={{ textTransform: "none", fontSize: "0.75rem", p: 0.25, minWidth: 0 }}
      >
        {label}
      </Button>
    );
  }

  return <Typography variant="caption" color="text.secondary">{label}</Typography>;
};

const AdminHistoryPage = () => {
  const [checkingAccess, setCheckingAccess] = useState(true);
  const [canAccess,      setCanAccess]      = useState(false);

  const [entries,     setEntries]     = useState<AdminHistoryEntry[]>([]);
  const [options,     setOptions]     = useState<HistoryActionOption[]>([]);
  const [entityTypes, setEntityTypes] = useState<string[]>([]);
  const [page,        setPage]        = useState(1);
  const [totalPages,  setTotalPages]  = useState(0);
  const [totalCount,  setTotalCount]  = useState(0);
  const [loading,     setLoading]     = useState(true);
  const [error,       setError]       = useState("");

  const [userOptions,  setUserOptions]  = useState<AdminUser[]>([]);
  const [selectedUser, setSelectedUser] = useState<AdminUser | null>(null);
  const [userSearch,   setUserSearch]   = useState("");
  const [userLoading,  setUserLoading]  = useState(false);

  const [search,     setSearch]     = useState("");
  const [role,       setRole]       = useState("");
  const [actionType, setActionType] = useState("");
  const [entityType, setEntityType] = useState("");
  const [fromDate,   setFromDate]   = useState("");
  const [toDate,     setToDate]     = useState("");
  const [sort,       setSort]       = useState<"newest" | "oldest">("newest");

  const [debouncedSearch,     setDebouncedSearch]     = useState("");
  const [debouncedUserSearch, setDebouncedUserSearch] = useState("");
  const isFirstLoad = useRef(true);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedUserSearch(userSearch), 400);
    return () => clearTimeout(timer);
  }, [userSearch]);

  useEffect(() => {
    (async () => {
      try {
        setCanAccess(await historyService.getAdminAccess());
      } catch {
        setCanAccess(false);
      } finally {
        setCheckingAccess(false);
      }
    })();
  }, []);

  useEffect(() => {
    if (!canAccess) return;
    (async () => {
      try {
        const [actions, entities] = await Promise.all([
          historyService.getActionTypes(),
          historyService.getEntityTypes(),
        ]);
        setOptions(actions);
        setEntityTypes(entities);
      } catch { /* filter lists are optional */ }
    })();
  }, [canAccess]);

  // Server-side user search — the full user list is never held client-side.
  useEffect(() => {
    if (!canAccess) return;
    (async () => {
      setUserLoading(true);
      try {
        setUserOptions(await adminService.getUsers(debouncedUserSearch || undefined));
      } catch { /* leave the previous options in place */ }
      finally { setUserLoading(false); }
    })();
  }, [canAccess, debouncedUserSearch]);

  const loadHistory = useCallback(async () => {
    if (!canAccess) return;
    setLoading(true);
    try {
      const data = await historyService.getAdmin({
        page,
        pageSize: PAGE_SIZE,
        sort,
        userId:     selectedUser?.userId || undefined,
        role:       role || undefined,
        entityType: entityType || undefined,
        actionType: actionType !== "" ? Number(actionType) : undefined,
        search:     debouncedSearch || undefined,
        fromDate:   fromDate || undefined,
        toDate:     toDate || undefined,
      });
      setEntries(data.entries);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
      setError("");
    } catch {
      setError("Failed to load history.");
    } finally {
      setLoading(false);
      isFirstLoad.current = false;
    }
  }, [
    canAccess, page, sort, selectedUser, role, entityType,
    actionType, debouncedSearch, fromDate, toDate,
  ]);

  useEffect(() => { loadHistory(); }, [loadHistory]);

  useEffect(() => {
    if (!isFirstLoad.current) setPage(1);
  }, [selectedUser, role, entityType, actionType, debouncedSearch, fromDate, toDate, sort]);

  const groupedOptions = useMemo(() => {
    const groups = new Map<string, HistoryActionOption[]>();
    options.forEach((o) => {
      const list = groups.get(o.category) ?? [];
      list.push(o);
      groups.set(o.category, list);
    });
    return groups;
  }, [options]);

  if (checkingAccess) {
    return <Box sx={{ p: 4, textAlign: "center" }}><CircularProgress /></Box>;
  }

  if (!canAccess) {
    return (
      <Box sx={{ p: 4 }}>
        <Alert severity="error" sx={{ borderRadius: 2 }}>
          Access denied. Administration history is restricted to the Super Admin.
        </Alert>
      </Box>
    );
  }

  const hasFilters = Boolean(
    selectedUser || role || entityType || actionType ||
    debouncedSearch || fromDate || toDate,
  );

  return (
    <Box>
      <Stack spacing={0.5} mb={3}>
        <Typography variant="h4" fontWeight={700}>Administration History</Typography>
        <Stack direction="row" alignItems="center" spacing={0.75}>
          <LockRounded sx={{ fontSize: 15, color: "text.secondary" }} />
          <Typography variant="body2" color="text.secondary">
            Immutable audit trail across all users. Read-only — records cannot be
            edited or removed.
          </Typography>
        </Stack>
      </Stack>

      <Card sx={{ borderRadius: 3, mb: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}>
        <CardContent sx={{ p: 2.5, "&:last-child": { pb: 2.5 } }}>
          <Stack spacing={2}>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <Autocomplete
                sx={{ minWidth: 260, flexGrow: 1 }}
                size="small"
                options={userOptions}
                value={selectedUser}
                loading={userLoading}
                onChange={(_, value) => setSelectedUser(value)}
                onInputChange={(_, value) => setUserSearch(value)}
                getOptionLabel={(u) => `${u.fullName} (${u.email})`}
                isOptionEqualToValue={(a, b) => a.userId === b.userId}
                filterOptions={(x) => x}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="User"
                    placeholder="Search users..."
                    InputProps={{
                      ...params.InputProps,
                      endAdornment: (
                        <>
                          {userLoading ? <CircularProgress size={16} /> : null}
                          {params.InputProps.endAdornment}
                        </>
                      ),
                    }}
                  />
                )}
              />

              <TextField
                size="small"
                label="Keyword"
                placeholder="Search description..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                sx={{ minWidth: 220, flexGrow: 1 }}
              />

              <TextField
                select size="small" label="Role" value={role}
                onChange={(e) => setRole(e.target.value)}
                sx={{ minWidth: 150 }}
              >
                <MenuItem value="">All roles</MenuItem>
                {ROLES.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
              </TextField>
            </Stack>

            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                select size="small" label="Action" value={actionType}
                onChange={(e) => setActionType(e.target.value)}
                sx={{ minWidth: 210, flexGrow: 1 }}
              >
                <MenuItem value="">All actions</MenuItem>
                {[...groupedOptions.entries()].map(([category, items]) => [
                  <MenuItem key={`${category}-header`} disabled sx={{ opacity: 0.7 }}>
                    {category}
                  </MenuItem>,
                  ...items.map((o) => (
                    <MenuItem key={o.actionType} value={String(o.actionType)} sx={{ pl: 3 }}>
                      {o.actionName}
                    </MenuItem>
                  )),
                ])}
              </TextField>

              <TextField
                select size="small" label="Entity" value={entityType}
                onChange={(e) => setEntityType(e.target.value)}
                sx={{ minWidth: 170 }}
              >
                <MenuItem value="">All entities</MenuItem>
                {entityTypes.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
              </TextField>

              <TextField
                size="small" type="date" label="From" value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
                InputLabelProps={{ shrink: true }} sx={{ minWidth: 160 }}
              />
              <TextField
                size="small" type="date" label="To" value={toDate}
                onChange={(e) => setToDate(e.target.value)}
                InputLabelProps={{ shrink: true }} sx={{ minWidth: 160 }}
              />

              <TextField
                select size="small" label="Sort" value={sort}
                onChange={(e) => setSort(e.target.value as "newest" | "oldest")}
                sx={{ minWidth: 150 }}
              >
                <MenuItem value="newest">Newest first</MenuItem>
                <MenuItem value="oldest">Oldest first</MenuItem>
              </TextField>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      {error && <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>}

      {selectedUser && (
        <Alert severity="info" sx={{ mb: 2, borderRadius: 2 }}>
          Viewing history for <strong>{selectedUser.fullName}</strong> ({selectedUser.email}).
        </Alert>
      )}

      {loading ? (
        <Stack spacing={1.5}>
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} variant="rounded" height={52} sx={{ borderRadius: 2 }} />
          ))}
        </Stack>
      ) : entries.length === 0 ? (
        <EmptyState
          title={hasFilters ? "No matching activity." : "No activity recorded yet."}
          description={
            hasFilters
              ? "Try a different user, action, entity or date range."
              : "Audited actions will appear here as the platform is used."
          }
          icon={<HistoryRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <>
          <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: "block" }}>
            {totalCount} {totalCount === 1 ? "entry" : "entries"}
          </Typography>

          <TableContainer
            component={Card}
            sx={{ borderRadius: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}
          >
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 700 }}>Timestamp</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>User</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Role</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Action</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Description</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Entity</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entries.map((entry) => {
                  const color = categoryColor(entry.category);
                  return (
                    <TableRow key={entry.id} hover>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>
                        <Typography variant="caption">
                          {formatDateTime(entry.timestamp)}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="caption">
                          {entry.username ?? "—"}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Typography variant="caption" color="text.secondary">
                          {entry.role ?? "—"}
                        </Typography>
                      </TableCell>
                      <TableCell sx={{ whiteSpace: "nowrap" }}>
                        <Chip
                          label={entry.actionName}
                          size="small"
                          sx={{
                            height: 20, fontSize: "0.68rem", fontWeight: 600,
                            color, bgcolor: `${color}1A`,
                          }}
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="caption">{entry.description}</Typography>
                      </TableCell>
                      <TableCell>
                        <EntityCell entry={entry} />
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>

          <PaginationBar page={page} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </Box>
  );
};

export default AdminHistoryPage;
