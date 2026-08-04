import { useCallback, useEffect, useRef, useState } from "react";
import {
  Alert, Avatar, Box, Button, Card, CardContent, Chip, CircularProgress,
  Dialog, DialogContent, DialogTitle, Divider, IconButton, Skeleton, Snackbar,
  Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography,
} from "@mui/material";
import {
  CloseRounded, DeleteRounded, SearchRounded, VisibilityRounded,
  WorkspacePremiumRounded,
} from "@mui/icons-material";
import { adminService } from "../../services/adminService";
import { AdminCertificate } from "../../types/admin.types";
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

const DetailRow = ({ label, value }: { label: string; value: string }) => (
  <Box>
    <Typography variant="caption" color="text.secondary">{label}</Typography>
    <Typography variant="body2" fontWeight={600}>{value}</Typography>
  </Box>
);

const AdminCertificatesPage = () => {
  const [entries, setEntries] = useState<AdminCertificate[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");

  const [viewTarget, setViewTarget] = useState<AdminCertificate | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AdminCertificate | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const { error, handleError, clearError } = useApiError();
  const isFirstLoad = useRef(true);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 400);
    return () => clearTimeout(timer);
  }, [search]);

  const load = useCallback(async () => {
    setLoading(true);
    clearError();
    try {
      const data = await adminService.getCertificates({
        page,
        pageSize: PAGE_SIZE,
        search: debouncedSearch || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
      });
      setEntries(data.entries);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err) { handleError(err); }
    finally { setLoading(false); isFirstLoad.current = false; }
  }, [page, debouncedSearch, fromDate, toDate, clearError, handleError]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    if (!isFirstLoad.current) setPage(1);
  }, [debouncedSearch, fromDate, toDate]);

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    clearError();
    try {
      await adminService.deleteCertificate(deleteTarget.id);
      setDeleteTarget(null);
      setSuccessMsg(`Certificate ${deleteTarget.certificateNumber} deleted.`);
      await load();
    } catch (err) { handleError(err); }
    finally { setDeleting(false); }
  };

  const hasFilters = Boolean(debouncedSearch || fromDate || toDate);

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Certificates</Typography>
          <Typography variant="body2" color="text.secondary">
            View and revoke certificates issued to users.
          </Typography>
        </Box>
      </Stack>

      <Card sx={{ borderRadius: 3, mb: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}>
        <CardContent sx={{ p: 2.5, "&:last-child": { pb: 2.5 } }}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField
              size="small"
              label="Search"
              placeholder="User, email or learning path..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              sx={{ minWidth: 260, flexGrow: 1 }}
              InputProps={{ startAdornment: <SearchRounded sx={{ mr: 1, color: "text.disabled", fontSize: 18 }} /> }}
            />
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
          title={hasFilters ? "No matching certificates." : "No certificates issued yet."}
          description={
            hasFilters
              ? "Try a different search term or date range."
              : "Certificates will appear here as users complete learning paths."
          }
          icon={<WorkspacePremiumRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <>
          <Typography variant="caption" color="text.secondary" sx={{ mb: 1.5, display: "block" }}>
            {totalCount} {totalCount === 1 ? "certificate" : "certificates"}
          </Typography>

          <TableContainer
            component={Card}
            sx={{ borderRadius: 3, boxShadow: "0 2px 14px rgba(0,0,0,0.05)" }}
          >
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ fontWeight: 700 }}>Certificate #</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>User</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Learning Path</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Issued</TableCell>
                  <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {entries.map((c) => (
                  <TableRow key={c.id} hover>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      <Typography variant="body2" fontWeight={600}>
                        {c.certificateNumber}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Stack direction="row" alignItems="center" spacing={1}>
                        <Avatar sx={{ width: 28, height: 28, fontSize: "0.7rem", fontWeight: 700 }}>
                          {c.userName.split(" ").map((n) => n[0]).join("")}
                        </Avatar>
                        <Box>
                          <Typography variant="body2">{c.userName}</Typography>
                          <Typography variant="caption" color="text.secondary">{c.userEmail}</Typography>
                        </Box>
                      </Stack>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{c.learningPathTitle}</Typography>
                    </TableCell>
                    <TableCell sx={{ whiteSpace: "nowrap" }}>
                      <Typography variant="caption">{formatDateTime(c.issuedAt)}</Typography>
                    </TableCell>
                    <TableCell>
                      <Chip label={c.status} size="small"
                        sx={{ height: 20, fontSize: "0.68rem", fontWeight: 600, color: "#10B981", bgcolor: "#10B9811A" }} />
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                        <Tooltip title="View details">
                          <IconButton size="small" onClick={() => setViewTarget(c)}>
                            <VisibilityRounded fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Delete certificate">
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

      <Dialog open={viewTarget !== null} onClose={() => setViewTarget(null)} maxWidth="sm" fullWidth
        PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Stack direction="row" spacing={1} alignItems="center">
              <WorkspacePremiumRounded sx={{ color: "#F59E0B" }} />
              <Typography variant="h6" fontWeight={700}>
                {viewTarget?.certificateNumber}
              </Typography>
            </Stack>
            <IconButton onClick={() => setViewTarget(null)} size="small"><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          {viewTarget && (
            <Stack spacing={2}>
              <DetailRow label="User" value={`${viewTarget.userName} (${viewTarget.userEmail})`} />
              <DetailRow label="Learning Path" value={viewTarget.learningPathTitle} />
              <Divider />
              <DetailRow label="Certificate Number" value={viewTarget.certificateNumber} />
              <DetailRow label="Certificate URL" value={viewTarget.certificateUrl} />
              <DetailRow label="Issued At" value={formatDateTime(viewTarget.issuedAt)} />
              <DetailRow label="Completed At" value={formatDateTime(viewTarget.completedAt)} />
              <DetailRow label="Status" value={viewTarget.status} />
              <Divider />
              <Alert severity="warning" sx={{ borderRadius: 2 }}>
                Deleting this certificate revokes it and unblocks deletion of the
                learning path "{viewTarget.learningPathTitle}".
              </Alert>
            </Stack>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete Certificate"
        message={`Revoke ${deleteTarget?.certificateNumber} issued to "${deleteTarget?.userName}" for "${deleteTarget?.learningPathTitle}"? This cannot be undone.`}
        confirmLabel="Delete"
        confirmLoading={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </Box>
  );
};

export default AdminCertificatesPage;
