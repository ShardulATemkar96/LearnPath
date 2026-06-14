import {
  Box, Button, Dialog, DialogContent, DialogTitle,
  FormControlLabel, IconButton, Stack, Switch,
  TextField, Typography, CircularProgress,
} from "@mui/material";
import { CloseRounded } from "@mui/icons-material";
import { useState } from "react";
import { useDispatch } from "react-redux";
import { AppDispatch } from "../../../redux/store";
import { createPathThunk } from "../../../redux/slices/pathSlice";

interface Props {
  open: boolean;
  onClose: () => void;
}

const CreatePathModal = ({ open, onClose }: Props) => {
  const dispatch = useDispatch<AppDispatch>();
  const [loading, setLoading] = useState(false);
  const [form, setForm] = useState({
    title: "", description: "", thumbnailUrl: "", isPublic: false,
  });
  const [errors, setErrors] = useState<{ title?: string; description?: string }>({});

  const validate = () => {
    const e: typeof errors = {};
    if (!form.title.trim()) e.title = "Title is required.";
    if (!form.description.trim()) e.description = "Description is required.";
    return e;
  };

  const handleSubmit = async () => {
    const e = validate();
    if (Object.keys(e).length > 0) { setErrors(e); return; }
    setErrors({});
    setLoading(true);
    await dispatch(createPathThunk({
      title: form.title,
      description: form.description,
      thumbnailUrl: form.thumbnailUrl || undefined,
      isPublic: form.isPublic,
    }));
    setLoading(false);
    setForm({ title: "", description: "", thumbnailUrl: "", isPublic: false });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>New Learning Path</Typography>
          <IconButton onClick={onClose} size="small">
            <CloseRounded />
          </IconButton>
        </Stack>
      </DialogTitle>

      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          <TextField
            label="Title" fullWidth value={form.title}
            onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))}
            error={!!errors.title} helperText={errors.title}
          />
          <TextField
            label="Description" fullWidth multiline rows={3}
            value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
            error={!!errors.description} helperText={errors.description}
          />
          <TextField
            label="Thumbnail URL (optional)" fullWidth value={form.thumbnailUrl}
            onChange={(e) => setForm((p) => ({ ...p, thumbnailUrl: e.target.value }))}
          />
          <FormControlLabel
            control={
              <Switch
                checked={form.isPublic}
                onChange={(e) => setForm((p) => ({ ...p, isPublic: e.target.checked }))}
                color="primary"
              />
            }
            label="Make this path public"
          />
          <Box pt={1}>
            <Button
              variant="contained" fullWidth size="large"
              onClick={handleSubmit} disabled={loading}
              sx={{ background: "linear-gradient(135deg, #6C63FF, #9D97FF)", borderRadius: 2 }}
            >
              {loading ? <CircularProgress size={22} sx={{ color: "#fff" }} /> : "Create Path"}
            </Button>
          </Box>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default CreatePathModal;
