import {
  Button, Dialog, DialogActions, DialogContent,
  DialogTitle, Typography,
} from "@mui/material";
import { WarningAmberRounded } from "@mui/icons-material";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
  severity?: "error" | "warning";
}

const ConfirmDialog = ({
  open, title, message,
  confirmLabel = "Confirm",
  cancelLabel  = "Cancel",
  onConfirm, onCancel,
  severity = "error",
}: ConfirmDialogProps) => (
  <Dialog
    open={open}
    onClose={onCancel}
    maxWidth="xs"
    fullWidth
    PaperProps={{ sx: { borderRadius: 4 } }}
  >
    <DialogTitle sx={{ pb: 1 }}>
      <Typography variant="h6" fontWeight={700}
        sx={{ display: "flex", alignItems: "center", gap: 1 }}>
        <WarningAmberRounded
          sx={{ color: severity === "error" ? "error.main" : "warning.main" }}
        />
        {title}
      </Typography>
    </DialogTitle>

    <DialogContent>
      <Typography variant="body2" color="text.secondary">
        {message}
      </Typography>
    </DialogContent>

    <DialogActions sx={{ px: 3, pb: 3, gap: 1 }}>
      <Button onClick={onCancel} sx={{ borderRadius: 2 }}>
        {cancelLabel}
      </Button>
      <Button
        variant="contained"
        onClick={onConfirm}
        color={severity}
        sx={{ borderRadius: 2 }}
      >
        {confirmLabel}
      </Button>
    </DialogActions>
  </Dialog>
);

export default ConfirmDialog;
