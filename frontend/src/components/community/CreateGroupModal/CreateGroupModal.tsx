import { useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import {
  Alert, Button, CircularProgress, Dialog, DialogContent, DialogTitle,
  FormControlLabel, IconButton, Stack, Switch, TextField, Typography,
} from "@mui/material";
import { CloseRounded } from "@mui/icons-material";
import { AppDispatch } from "../../../redux/store";
import {
  createGroupThunk, updateGroupThunk,
} from "../../../redux/slices/communitySlice";
import { Group } from "../../../types/community.types";

interface CreateGroupModalProps {
  open: boolean;
  onClose: () => void;
  group?: Group | null;
  onSaved?: () => void;
}

const CreateGroupModal = ({
  open, onClose, group, onSaved,
}: CreateGroupModalProps) => {
  const dispatch = useDispatch<AppDispatch>();
  const isEdit = !!group;

  const [form, setForm] = useState({
    name: "", description: "", isPublic: true,
  });
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState("");

  useEffect(() => {
    if (open) {
      setForm({
        name:        group?.name ?? "",
        description: group?.description ?? "",
        isPublic:    group?.isPublic ?? true,
      });
      setError("");
    }
  }, [open, group]);

  const validate = () => {
    if (!form.name.trim())        return "Group name is required.";
    if (form.name.trim().length > 100)
      return "Group name cannot exceed 100 characters.";
    if (!form.description.trim()) return "Group description is required.";
    return "";
  };

  const handleSubmit = async () => {
    const err = validate();
    if (err) { setError(err); return; }

    const payload = {
      name:        form.name.trim(),
      description: form.description.trim(),
      isPublic:    form.isPublic,
    };

    setError(""); setLoading(true);

    const result = isEdit && group
      ? await dispatch(updateGroupThunk({ groupId: group.id, payload }))
      : await dispatch(createGroupThunk(payload));

    setLoading(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      onSaved?.();
      onClose();
    } else {
      setError((result as any).payload ?? "Failed to save group.");
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>
            {isEdit ? "Edit Group" : "Create Group"}
          </Typography>
          <IconButton onClick={onClose} size="small">
            <CloseRounded />
          </IconButton>
        </Stack>
      </DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} pt={1}>
          {error && (
            <Alert severity="error" sx={{ borderRadius: 2 }}>{error}</Alert>
          )}
          <TextField
            label="Group name" fullWidth value={form.name}
            onChange={(e) => setForm((p) => ({ ...p, name: e.target.value }))}
            placeholder="e.g. React Enthusiasts"
            inputProps={{ maxLength: 100 }}
          />
          <TextField
            label="Description" fullWidth value={form.description}
            onChange={(e) => setForm((p) => ({ ...p, description: e.target.value }))}
            placeholder="What is this group about?"
            multiline minRows={3}
            inputProps={{ maxLength: 1000 }}
          />
          <FormControlLabel
            control={
              <Switch
                checked={form.isPublic}
                onChange={(e) => setForm((p) => ({ ...p, isPublic: e.target.checked }))}
              />
            }
            label="Public group"
          />
          <Stack direction="row" justifyContent="flex-end" spacing={1.5} pt={1}>
            <Button onClick={onClose} sx={{ borderRadius: 2 }}>
              Cancel
            </Button>
            <Button
              variant="contained"
              onClick={handleSubmit}
              disabled={loading}
              startIcon={loading ? <CircularProgress size={18} /> : undefined}
              sx={{
                borderRadius: 2,
                background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
              }}
            >
              {isEdit ? "Save Changes" : "Create Group"}
            </Button>
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default CreateGroupModal;
