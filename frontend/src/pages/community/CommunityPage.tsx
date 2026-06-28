import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, CircularProgress,
  Dialog, DialogContent, DialogTitle, IconButton,
  MenuItem, Select, Skeleton, Stack,
  Tab, Tabs, TextField, Typography,
} from "@mui/material";
import { AddRounded, CloseRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchPosts, createPostThunk,
} from "../../redux/slices/communitySlice";
import {
  selectPosts, selectCommunityLoading,
  selectCommunityError, selectTotalPages,
  selectCurrentPage,
} from "../../redux/selectors/communitySelectors";
import { COMMUNITY_CATEGORIES } from "../../types/community.types";
import PostCard   from "../../components/community/PostCard/PostCard";
import SearchBar  from "../../components/common/SearchBar/SearchBar";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import { GroupsRounded } from "@mui/icons-material";
import { useDebounce } from "../../hooks/useDebounce";

// ── Create Post Modal ─────────────────────────────────────────
const CreatePostModal = ({
  open, onClose,
}: { open: boolean; onClose: () => void }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [form, setForm] = useState({
    title: "", content: "", category: "General",
  });
  const [loading, setLoading] = useState(false);
  const [error,   setError]   = useState("");

  const validate = () => {
    if (!form.title.trim())   return "Title is required.";
    if (!form.content.trim()) return "Content is required.";
    return "";
  };

  const handleSubmit = async () => {
    const err = validate();
    if (err) { setError(err); return; }
    setError(""); setLoading(true);
    const result = await dispatch(createPostThunk(form));
    setLoading(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setForm({ title: "", content: "", category: "General" });
      onClose();
    } else {
      setError((result as any).payload ?? "Failed to create post.");
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth
      PaperProps={{ sx: { borderRadius: 4 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Typography variant="h6" fontWeight={700}>New Post</Typography>
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
            label="Content" fullWidth multiline rows={5}
            value={form.content}
            onChange={(e) => setForm((p) => ({ ...p, content: e.target.value }))}
            placeholder="Share your thoughts, questions, or resources..."
          />
          <Button
            variant="contained" fullWidth size="large"
            onClick={handleSubmit} disabled={loading}
            sx={{
              background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
              borderRadius: 2,
            }}
          >
            {loading
              ? <CircularProgress size={22} sx={{ color: "#fff" }} />
              : "Post"}
          </Button>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

// ── Main Page ─────────────────────────────────────────────────
const CommunityPage = () => {
  const dispatch   = useDispatch<AppDispatch>();
  const posts      = useSelector(selectPosts);
  const loading    = useSelector(selectCommunityLoading);
  const error      = useSelector(selectCommunityError);
  const totalPages = useSelector(selectTotalPages);
  const page       = useSelector(selectCurrentPage);

  const [category,    setCategory]    = useState("All");
  const [search,      setSearch]      = useState("");
  const [createOpen,  setCreateOpen]  = useState(false);

  const debouncedSearch = useDebounce(search, 400);

  useEffect(() => {
    dispatch(fetchPosts({ category, search: debouncedSearch, page: 1 }));
  }, [category, debouncedSearch, dispatch]);

  const handlePageChange = (p: number) => {
    dispatch(fetchPosts({ category, search: debouncedSearch, page: p }));
  };

  return (
    <Box>
      {/* Header */}
      <Stack direction="row" alignItems="center"
        justifyContent="space-between" mb={4} flexWrap="wrap" gap={2}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Community</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Discuss, share, and learn together.
          </Typography>
        </Box>
        <Button
          variant="contained" startIcon={<AddRounded />}
          onClick={() => setCreateOpen(true)}
          sx={{
            background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
            borderRadius: 2,
          }}
        >
          New Post
        </Button>
      </Stack>

      {/* Search + Filter */}
      <Stack direction={{ xs: "column", sm: "row" }}
        spacing={2} mb={3} alignItems="center">
        <Box sx={{ flexGrow: 1, maxWidth: 420 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search posts..."
            fullWidth
          />
        </Box>

        <Stack direction="row" spacing={1} flexWrap="wrap">
          {COMMUNITY_CATEGORIES.map((cat) => (
            <Chip
              key={cat}
              label={cat}
              clickable
              onClick={() => setCategory(cat)}
              color={category === cat ? "primary" : "default"}
              sx={{
                fontWeight: 600,
                fontSize: "0.78rem",
                ...(category === cat && {
                  background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                  color: "#fff",
                }),
              }}
            />
          ))}
        </Stack>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      {/* Posts */}
      {loading ? (
        <Stack spacing={2}>
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} variant="rounded"
              height={140} sx={{ borderRadius: 3 }} />
          ))}
        </Stack>
      ) : posts.length === 0 ? (
        <EmptyState
          title="No posts yet."
          description="Be the first to start a discussion!"
          icon={<GroupsRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <Stack spacing={2}>
          {posts.map((post) => (
            <PostCard key={post.id} post={post} />
          ))}
        </Stack>
      )}

      <PaginationBar
        page={page}
        totalPages={totalPages}
        onChange={handlePageChange}
      />

      <CreatePostModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
    </Box>
  );
};

export default CommunityPage;
