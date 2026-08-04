import { useState } from "react";
import { useDispatch } from "react-redux";
import {
  Alert, Button, Dialog, DialogActions, DialogContent,
  DialogTitle, TextField, Typography,
} from "@mui/material";
import { FlagRounded } from "@mui/icons-material";
import { AppDispatch } from "../../../redux/store";
import {
  reportPostThunk, reportCommentThunk,
} from "../../../redux/slices/communitySlice";

interface ReportDialogProps {
  open: boolean;
  onClose: () => void;
  target: { type: "post"; id: number } | { type: "comment"; id: number };
  onReported: (msg: string) => void;
}

const ReportDialog = ({ open, onClose, target, onReported }: ReportDialogProps) => {
  const dispatch = useDispatch<AppDispatch>();
  const [reason, setReason] = useState("");
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!reason.trim()) {
      setError("Please provide a reason.");
      return;
    }
    setError(""); setSubmitting(true);
    const result = target.type === "post"
      ? await dispatch(reportPostThunk({ postId: target.id, payload: { reason } }))
      : await dispatch(reportCommentThunk({ commentId: target.id, payload: { reason } }));
    setSubmitting(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setReason("");
      onReported("Report submitted.");
      onClose();
    } else {
      setError((result as any).payload ?? "Failed to submit report.");
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Typography variant="h6" fontWeight={700}
          sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          <FlagRounded sx={{ color: "warning.main" }} />
          Report {target.type}
        </Typography>
      </DialogTitle>
      <DialogContent>
        <TextField
          label="Reason" fullWidth multiline rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="Explain why this should be reviewed..."
          autoFocus
          sx={{ "& .MuiOutlinedInput-root": { borderRadius: 2.5 } }}
        />
        {error && (
          <Alert severity="error" sx={{ mt: 1.5, borderRadius: 2 }}>{error}</Alert>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3, gap: 1 }}>
        <Button onClick={onClose} disabled={submitting} sx={{ borderRadius: 2 }}>
          Cancel
        </Button>
        <Button
          variant="contained" color="warning"
          onClick={handleSubmit} disabled={submitting}
          sx={{ borderRadius: 2 }}
        >
          {submitting ? "..." : "Submit Report"}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ReportDialog;
