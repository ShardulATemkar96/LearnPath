import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, MenuItem, Select, Skeleton, Snackbar,
  Stack, Typography,
} from "@mui/material";
import { AddRounded, GroupsRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import { fetchPosts, clearCommunityError } from "../../redux/slices/communitySlice";
import {
  selectPosts, selectCommunityLoading,
  selectCommunityError, selectTotalPages,
  selectCurrentPage,
} from "../../redux/selectors/communitySelectors";
import { PostFilter, PostSortOrder } from "../../types/community.types";
import PostCard from "../../components/community/PostCard/PostCard";
import CreatePostModal from "../../components/community/CreatePostModal/CreatePostModal";
import SearchBar from "../../components/common/SearchBar/SearchBar";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import CommunityNav from "../../components/community/CommunityNav/CommunityNav";
import { useDebounce } from "../../hooks/useDebounce";
import { useAuth } from "../../hooks/useAuth";

const CommunityMyPostsPage = () => {
  const dispatch   = useDispatch<AppDispatch>();
  const { user }   = useAuth();
  const posts      = useSelector(selectPosts);
  const loading    = useSelector(selectCommunityLoading);
  const error      = useSelector(selectCommunityError);
  const totalPages = useSelector(selectTotalPages);
  const page       = useSelector(selectCurrentPage);

  const [category,   setCategory]   = useState("All");
  const [search,     setSearch]     = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [toast,      setToast]      = useState("");
  const [sort,       setSort]       = useState<PostSortOrder>("Newest");
  const [filter] = useState<PostFilter>("My Posts");

  const debouncedSearch = useDebounce(search, 400);

  useEffect(() => {
    dispatch(clearCommunityError());
    const promise = dispatch(fetchPosts({
      category, search: debouncedSearch, page: 1, sort, filter,
    }));
    return () => {
      // Abort stale request when deps change or component unmounts
      (promise as any).abort?.();
    };
  }, [category, debouncedSearch, sort, filter, dispatch]);

  const handlePageChange = (p: number) => {
    dispatch(clearCommunityError());
    dispatch(fetchPosts({
      category, search: debouncedSearch, page: p, sort, filter,
    }));
  };

  return (
    <Box>
      <CommunityNav />

      {/* Header */}
      <Stack direction="row" alignItems="center"
        justifyContent="space-between" mb={4} flexWrap="wrap" gap={2}>
        <Box>
          <Typography variant="h4" fontWeight={700}>My Posts</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Posts you've created across all community groups.
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

      {/* Search + Controls */}
      <Stack direction={{ xs: "column", md: "row" }}
        spacing={2} mb={3} alignItems="center">
        <Box sx={{ flexGrow: 1, maxWidth: 420 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search your posts..."
            fullWidth
          />
        </Box>
        <Select
          value={sort}
          size="small"
          onChange={(e) => setSort(e.target.value as PostSortOrder)}
          sx={{ minWidth: 160, borderRadius: 2 }}
        >
          <MenuItem value="Newest">Newest</MenuItem>
          <MenuItem value="Score">Highest Score</MenuItem>
          <MenuItem value="Trending">Trending</MenuItem>
        </Select>
      </Stack>

      {/* Categories */}
      <Stack direction="row" spacing={1} mb={3} flexWrap="wrap" gap={1}>
        {["All", "General", "Questions", "Resources", "Projects", "Announcements"].map((cat) => (
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
          description={`You haven't created any posts yet, ${user?.firstName ?? ""}. Click "New Post" to get started.`}
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
        onSaved={() => setToast("Post published.")}
      />

      <Snackbar
        open={!!toast}
        autoHideDuration={3000}
        onClose={() => setToast("")}
        message={toast}
      />
    </Box>
  );
};

export default CommunityMyPostsPage;
