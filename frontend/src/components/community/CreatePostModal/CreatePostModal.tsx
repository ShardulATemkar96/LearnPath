import { useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import {
  Alert, Button, CircularProgress, Dialog, DialogContent, DialogTitle,
  Divider, FormControl, IconButton, InputLabel, MenuItem, Select,
  Stack, TextField, Typography,
} from "@mui/material";
import { CloseRounded } from "@mui/icons-material";
import { AppDispatch } from "../../../redux/store";
import {
  createPostThunk, createGroupPostThunk, updatePostThunk,
} from "../../../redux/slices/communitySlice";
import {
  PostDetail, COMMUNITY_CATEGORIES, PROGRAMMING_LANGUAGES,
} from "../../../types/community.types";

interface CreatePostModalProps {
  open: boolean;
  onClose: () => void;
  groupId?: number;
  post?: PostDetail | null;
  onSaved?: () => void;
}

const CreatePostModal = ({
  open, onClose, groupId, post, onSaved,
}: CreatePostModalProps) => {
  const dispatch = useDispatch<AppDispatch>();
  const isEdit = !!post;

  const [form, setForm] = useState({
    title: "", content: "", category: "General",
    codeSnippet: "", programmingLanguage: "", tags: "",
  });
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState("");

  useEffect(() => {
    if (open) {
      setForm({
        title:               post?.title ?? "",
        content:             post?.content ?? "",
        category:            post?.category ?? "General",
        codeSnippet:         post?.codeSnippet ?? "",
        programmingLanguage: post?.programmingLanguage ?? "",
        tags:                post?.tags ?? "",
      });
      setError("");
    }
  }, [open, post]);

  const validate = () => {
    if (!form.title.trim())   return "Title is required.";
    if (!form.content.trim()) return "Content is required.";
    if (form.codeSnippet.trim() && !form.programmingLanguage)
      return "Programming language is required when a code snippet is provided.";
    return "";
  };

  const handleSubmit = async () => {
    const err = validate();
    if (err) { setError(err); return; }

    const payload = {
      title:    form.title,
      content:  form.content,
      category: form.category,
      codeSnippet:         form.codeSnippet.trim() || null,
      programmingLanguage: form.programmingLanguage || null,
      tags:                form.tags.trim() || null,
    };

    setError(""); setLoading(true);

    let result;
    if (isEdit && post) {
      result = await dispatch(updatePostThunk({ postId: post.id, payload }));
    } else if (groupId) {
      result = await dispatch(createGroupPostThunk({ groupId, payload }));
    } else {
      result = await dispatch(createPostThunk(payload));
    }

    setLoading(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      onSaved?.();
      onClose();
    } else {
      setError((result as any).payload ?? "Failed to save post.");
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>
            {isEdit ? "Edit Post" : "New Post"}
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
            label="Title" fullWidth value={form.title}
            onChange={(e) => setForm((p) => ({ ...p, title: e.target.value }))}
            placeholder="What's on your mind?"
          />
          <Select
            value={form.category} fullWidth size="small"
            onChange={(e) => setForm((p) => ({ ...p, category: e.target.value }))}
          >
            {COMMUNITY_CATEGORIES.filter((c) => c !== "All").map((c) => (
              <MenuItem key={c} value={c}>{c}</MenuItem>
            ))}
          </Select>
          <TextField
            label="Content" fullWidth multiline rows={6}
            value={form.content}
            onChange={(e) => setForm((p) => ({ ...p, content: e.target.value }))}
            placeholder="Share your thoughts, questions, or resources..."
          />

          <Divider />

          <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
            <FormControl fullWidth size="small">
              <InputLabel id="code-language-label">Language</InputLabel>
              <Select
                labelId="code-language-label"
                label="Language"
                value={form.programmingLanguage}
                onChange={(e) => setForm((p) => ({ ...p, programmingLanguage: e.target.value }))}
              >
                {PROGRAMMING_LANGUAGES.map((lang) => (
                  <MenuItem key={lang} value={lang}>{lang}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Tags (comma separated)" fullWidth size="small"
              value={form.tags}
              onChange={(e) => setForm((p) => ({ ...p, tags: e.target.value }))}
              placeholder="react, redux, mui"
            />
          </Stack>
          <TextField
            label="Code Snippet (optional)" fullWidth multiline
            minRows={5} maxRows={12}
            value={form.codeSnippet}
            onChange={(e) => setForm((p) => ({ ...p, codeSnippet: e.target.value }))}
            placeholder="Paste your code here..."
            sx={{ "& textarea": { fontFamily: "'Consolas', 'Monaco', monospace", fontSize: "0.85rem" } }}
          />

          <Stack direction="row" spacing={1.5} justifyContent="flex-end">
            <Button
              variant="outlined" onClick={onClose} disabled={loading}
              sx={{ borderRadius: 2 }}
            >
              Cancel
            </Button>
            <Button
              variant="contained" onClick={handleSubmit} disabled={loading}
              sx={{
                borderRadius: 2,
                background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
              }}
            >
              {loading
                ? <CircularProgress size={22} sx={{ color: "#fff" }} />
                : isEdit ? "Save Changes" : "Post"}
            </Button>
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default CreatePostModal;
