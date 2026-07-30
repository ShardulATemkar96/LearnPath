import { useEffect, useState, useCallback, Fragment  } from "react";
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogContent, DialogTitle,
  IconButton, Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  TextField, Tooltip, Typography, Paper,
} from "@mui/material";
import { questionBankService } from "../../services/questionBankService";
import {
  AddRounded, ArchiveRounded, CloudDownloadRounded, CloseRounded, CloudUploadRounded,
  ContentCopyRounded, DeleteRounded, RestoreRounded, SearchRounded,
} from "@mui/icons-material";
import {
  DialogActions, DialogContentText,
} from "@mui/material";

const JSON_TEMPLATE = `{
  "title": "Java OOP",
  "subject": "Java",
  "questions": [
    {
      "question": "Which keyword inherits a class?",
      "options": ["extends", "implements", "inherits", "super"],
      "correct_answer": "extends",
      "difficulty": "Easy",
      "explanation": "Java uses extends for inheritance."
    }
  ]
}`;

const VALIDATION_RULES = [
  "File must be .json (max 5 MB)",
  "All fields are mandatory",
  "No empty strings",
  "options must contain at least 2 items",
  "correct_answer must exactly match one option (case-insensitive)",
  "difficulty must be Easy, Medium, or Hard (case-insensitive)",
  "explanation is required for every question",
  "Duplicate question text is not allowed within the same upload",
];
function highlightJson(text: string): React.ReactNode {
  const lines = text.split("\n");
  const out: React.ReactNode[] = [];
  lines.forEach((line, i) => {
    if (i > 0) out.push("\n");
    const indent = line.match(/^ */)?.[0] ?? "";
    out.push(<span key={`i${i}`} style={{ color: "#858585" }}>{indent}</span>);
    const rest = line.slice(indent.length);
    const keyMatch = rest.match(/^"([^"]+)"\s*:/);
    if (keyMatch) {
      out.push(<span key={`k${i}`} style={{ color: "#9cdcfe" }}>"{keyMatch[1]}"</span>);
      const val = rest.slice(keyMatch[0].length);
      const strMatch = val.match(/^ "([^"]*)"(,?)/);
      if (strMatch) {
        out.push(<span key={`c${i}`} style={{ color: "#d4d4d4" }}>: </span>);
        out.push(<span key={`s${i}`} style={{ color: "#ce9178" }}>"{strMatch[1]}"</span>);
        if (strMatch[2]) out.push(<span key={`t${i}`} style={{ color: "#d4d4d4" }}>{strMatch[2]}</span>);
      } else if (val.startsWith(" [")) {
        out.push(<span key={`c${i}`} style={{ color: "#d4d4d4" }}>: </span>);
        out.push(<span key={`a${i}`} style={{ color: "#ce9178" }}>{val.trim()}</span>);
      } else {
        out.push(<span key={`x${i}`} style={{ color: "#d4d4d4" }}>{val}</span>);
      }
    } else {
      let color = "#569cd6";
      if (!/[{}[\]]/.test(rest)) color = "#d4d4d4";
      out.push(<span key={`r${i}`} style={{ color }}>{rest}</span>);
    }
  });
  return <>{out}</>;
}
import { QuestionBankSummaryDto, QuestionBankStatus } from "../../types/questionBank.types";

const STATUS_LABEL: Record<QuestionBankStatus, string> = {
  [QuestionBankStatus.Draft]: "Draft",
  [QuestionBankStatus.Active]: "Active",
  [QuestionBankStatus.Archived]: "Archived",
};

const STATUS_COLOR: Record<QuestionBankStatus, "default" | "success" | "error"> = {
  [QuestionBankStatus.Draft]: "default",
  [QuestionBankStatus.Active]: "success",
  [QuestionBankStatus.Archived]: "error",
};

const AdminQuestionBanksPage = () => {
  const [banks, setBanks] = useState<QuestionBankSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");

  const [uploadOpen, setUploadOpen] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  const [versionOpen, setVersionOpen] = useState(false);
  const [versionBankId, setVersionBankId] = useState<number | null>(null);
  const [versionFile, setVersionFile] = useState<File | null>(null);
  const [copied, setCopied] = useState(false);

  const [deleteTarget, setDeleteTarget] = useState<QuestionBankSummaryDto | null>(null);
  const [deleting, setDeleting] = useState(false);

  const [restoreTarget, setRestoreTarget] = useState<QuestionBankSummaryDto | null>(null);
  const [restoring, setRestoring] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const handleCopyTemplate = useCallback(() => {
    navigator.clipboard.writeText(JSON_TEMPLATE).then(() => setCopied(true));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await questionBankService.search({ search: search || undefined });
      setBanks(result.items);
    } catch { setError("Failed to load question banks."); }
    finally { setLoading(false); }
  }, [search]);

  useEffect(() => { load(); }, [load]);

  const handleUpload = async () => {
    if (!uploadFile) return;
    setUploading(true);
    setError("");
    try {
      await questionBankService.upload(uploadFile);
      setUploadOpen(false);
      setUploadFile(null);
      load();
    } catch (e: any) {
      const serverMsg = e?.response?.data;
      if (serverMsg?.errors?.length > 0) {
        const detail = serverMsg.errors.join("; ");
        setError(`${serverMsg.message || "Validation failed."} ${detail}`);
      } else {
        setError(serverMsg?.message || "Upload failed.");
      }
    } finally { setUploading(false); }
  };

  const handleVersionUpload = async () => {
    if (!versionFile || !versionBankId) return;
    setUploading(true);
    setError("");
    try {
      await questionBankService.uploadVersion(versionBankId, versionFile);
      setVersionOpen(false);
      setVersionFile(null);
      setVersionBankId(null);
      load();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Version upload failed.");
    } finally { setUploading(false); }
  };

  const handleArchive = async (id: number) => {
    setError("");
    try {
      await questionBankService.archive(id);
      load();
    } catch (e: any) {
      setError(e?.response?.data?.message || "Archive failed.");
    }
  };

  const handleDownload = async (id: number, fileName: string) => {
    try {
      const blob = await questionBankService.download(id);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      a.click();
      URL.revokeObjectURL(url);
    } catch { setError("Download failed."); }
  };

  const handleRestoreConfirm = async () => {
    if (!restoreTarget) return;
    setRestoring(true);
    setError("");
    try {
      await questionBankService.restore(restoreTarget.id);
      setRestoreTarget(null);
      setSuccessMsg(`"${restoreTarget.title}" restored successfully.`);
      load();
    } catch (e: any) {
      const serverMsg = e?.response?.data;
      setError(serverMsg?.message || "Unable to restore Question Bank.");
      setRestoreTarget(null);
    } finally { setRestoring(false); }
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    setError("");
    try {
      await questionBankService.delete(deleteTarget.id);
      setDeleteTarget(null);
      load();
    } catch (e: any) {
      const serverMsg = e?.response?.data;
      setError(serverMsg?.message || "Delete failed.");
      setDeleteTarget(null);
    } finally { setDeleting(false); }
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h5" fontWeight={700}>Question Banks</Typography>
        <Button variant="contained" startIcon={<CloudUploadRounded />} onClick={() => setUploadOpen(true)}
          sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
          Upload JSON
        </Button>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError("")}>{error}</Alert>}
      <Snackbar open={!!successMsg} autoHideDuration={3000} onClose={() => setSuccessMsg("")}
        message={successMsg} anchorOrigin={{ vertical: "bottom", horizontal: "center" }} />

      <TextField size="small" placeholder="Search by title or subject..." value={search}
        onChange={(e) => setSearch(e.target.value)}
        InputProps={{ startAdornment: <SearchRounded sx={{ mr: 1, color: "text.secondary" }} /> }}
        sx={{ mb: 2, width: 360 }} />

      {loading ? (
        <Box sx={{ textAlign: "center", py: 8 }}><CircularProgress /></Box>
      ) : banks.length === 0 ? (
        <Box sx={{ textAlign: "center", py: 8, color: "text.secondary" }}>
          <CloudUploadRounded sx={{ fontSize: 64, mb: 2, opacity: 0.3 }} />
          <Typography variant="h6">No question banks found</Typography>
          <Typography variant="body2">Upload a JSON file to get started.</Typography>
        </Box>
      ) : (
        <TableContainer component={Paper} sx={{ borderRadius: 2 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Subject</TableCell>
                <TableCell>Version</TableCell>
                <TableCell>Questions</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Created</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {banks.map((bank) => (
                <TableRow key={bank.id}>
                  <TableCell><Typography fontWeight={600}>{bank.title}</Typography></TableCell>
                  <TableCell>{bank.subject}</TableCell>
                  <TableCell>v{bank.version}</TableCell>
                  <TableCell>{bank.questionCount}</TableCell>
                  <TableCell>
                    <Chip label={STATUS_LABEL[bank.status]} color={STATUS_COLOR[bank.status]} size="small" />
                  </TableCell>
                  <TableCell>{new Date(bank.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell align="right">
                    <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                      <IconButton size="small" title="Download" onClick={() => handleDownload(bank.id, bank.originalFileName)}>
                        <CloudDownloadRounded fontSize="small" />
                      </IconButton>
                      <IconButton size="small" title="Upload New Version"
                        onClick={() => { setVersionBankId(bank.id); setVersionOpen(true); }}>
                        <AddRounded fontSize="small" />
                      </IconButton>
                      {bank.status === QuestionBankStatus.Archived ? (
                        <IconButton size="small" title="Restore" color="success"
                          onClick={() => setRestoreTarget(bank)}>
                          <RestoreRounded fontSize="small" />
                        </IconButton>
                      ) : (
                        <IconButton size="small" title="Archive" color="error"
                          onClick={() => handleArchive(bank.id)}>
                          <ArchiveRounded fontSize="small" />
                        </IconButton>
                      )}
                      <IconButton size="small" title="Delete" color="error"
                        onClick={() => setDeleteTarget(bank)}>
                        <DeleteRounded fontSize="small" />
                      </IconButton>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={uploadOpen} onClose={() => { if (!uploading) setUploadOpen(false); }}
        maxWidth="md" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" fontWeight={700}>Upload Question Bank</Typography>
            <IconButton onClick={() => setUploadOpen(false)} size="small" disabled={uploading}><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <Typography variant="subtitle1" fontWeight={600}>Expected JSON Format</Typography>
            <Typography variant="body2" color="text.secondary">
              Please ensure your JSON file matches the structure shown below before uploading. Files with incorrect formatting will fail to process.
            </Typography>

            <Box sx={{ position: "relative", bgcolor: "#1e1e1e", borderRadius: 2, overflow: "hidden" }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center"
                sx={{ px: 2, py: 1, bgcolor: "#2d2d2d", borderBottom: "1px solid #3c3c3c" }}>
                <Typography variant="caption" sx={{ color: "#858585" }}>template.json</Typography>
                <Tooltip title={copied ? "Copied!" : "Copy Template"}>
                  <IconButton size="small" onClick={handleCopyTemplate} sx={{ color: "#858585", "&:hover": { color: "#fff" } }}>
                    <ContentCopyRounded fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Stack>
              <Box sx={{ p: 2, overflow: "auto", fontFamily: "'Cascadia Code','Fira Code','JetBrains Mono',Consolas,monospace", fontSize: 13, lineHeight: 1.6, whiteSpace: "pre" }}>
                {highlightJson(JSON_TEMPLATE)}
              </Box>
            </Box>

            <Box>
              <Typography variant="subtitle2" fontWeight={600} mb={1}>Validation Rules</Typography>
              <Stack spacing={0.5}>
                {VALIDATION_RULES.map((rule, i) => (
                  <Stack key={i} direction="row" spacing={1} alignItems="center">
                    <Typography variant="body2" sx={{ color: "success.main", fontWeight: 700 }}>✓</Typography>
                    <Typography variant="body2" color="text.secondary">{rule}</Typography>
                  </Stack>
                ))}
              </Stack>
            </Box>

            <TextField type="file" InputProps={{ inputProps: { accept: ".json" } }}
              onChange={(e: any) => setUploadFile(e.target.files?.[0] || null)}
              helperText="Only .json files up to 5MB are accepted." />
            <Button variant="contained" fullWidth size="large" onClick={handleUpload}
              disabled={!uploadFile || uploading}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              {uploading ? "Uploading..." : "Upload"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>

      <Snackbar open={copied} autoHideDuration={2000} onClose={() => setCopied(false)}
        message="Template copied to clipboard" anchorOrigin={{ vertical: "bottom", horizontal: "center" }} />

      <Dialog open={versionOpen} onClose={() => { if (!uploading) setVersionOpen(false); }}
        maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6" fontWeight={700}>Upload New Version</Typography>
            <IconButton onClick={() => setVersionOpen(false)} size="small" disabled={uploading}><CloseRounded /></IconButton>
          </Stack>
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2.5} pt={1}>
            <TextField type="file" InputProps={{ inputProps: { accept: ".json" } }}
              onChange={(e: any) => setVersionFile(e.target.files?.[0] || null)}
              helperText="Upload a new JSON file to create a new version." />
            <Button variant="contained" fullWidth size="large" onClick={handleVersionUpload}
              disabled={!versionFile || uploading}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}>
              {uploading ? "Uploading..." : "Upload Version"}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteTarget !== null} onClose={() => { if (!deleting) setDeleteTarget(null); }}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Typography variant="h6" fontWeight={700}>Delete Question Bank</Typography>
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete '{deleteTarget?.title}'? This action cannot be undone and will permanently remove the question bank and all associated questions.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setDeleteTarget(null)} disabled={deleting}
            sx={{ borderRadius: 2, color: "text.secondary" }}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleDelete} disabled={deleting}
            sx={{ borderRadius: 2 }}>
            {deleting ? "Deleting..." : "Delete"}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={restoreTarget !== null} onClose={() => { if (!restoring) setRestoreTarget(null); }}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 4 } }}>
        <DialogTitle>
          <Typography variant="h6" fontWeight={700}>Restore Question Bank</Typography>
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to restore this Question Bank? It will become available for normal use again.
          </DialogContentText>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setRestoreTarget(null)} disabled={restoring}
            sx={{ borderRadius: 2, color: "text.secondary" }}>Cancel</Button>
          <Button variant="contained" color="success" onClick={handleRestoreConfirm} disabled={restoring}
            sx={{ borderRadius: 2 }}>
            {restoring ? "Restoring..." : "Restore"}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AdminQuestionBanksPage;
