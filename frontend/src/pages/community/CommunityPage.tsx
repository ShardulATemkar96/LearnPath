import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, MenuItem, Select, Skeleton, Snackbar,
  Stack, Typography,
} from "@mui/material";
import { AddRounded, GroupsRounded, TagRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchPosts,
} from "../../redux/slices/communitySlice";
import {
  selectPosts, selectCommunityLoading,
  selectCommunityError, selectTotalPages,
  selectCurrentPage,
} from "../../redux/selectors/communitySelectors";
import {
  COMMUNITY_CATEGORIES, POST_FILTERS, PostFilter, PostSortOrder,
} from "../../types/community.types";
import PostCard   from "../../components/community/PostCard/PostCard";
import CreatePostModal from "../../components/community/CreatePostModal/CreatePostModal";
import SearchBar  from "../../components/common/SearchBar/SearchBar";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import EmptyState from "../../components/common/EmptyState/EmptyState";
import CommunityNav from "../../components/community/CommunityNav/CommunityNav";
import { useDebounce } from "../../hooks/useDebounce";

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
  const [toast,       setToast]       = useState("");
  const [sort,        setSort]        = useState<PostSortOrder>("Newest");
  const [filter,      setFilter]      = useState<PostFilter>("All Posts");
  const [tag,         setTag]         = useState("");

  const debouncedSearch = useDebounce(search, 400);

  useEffect(() => {
    dispatch(fetchPosts({
      category, search: debouncedSearch, page: 1, sort, filter, tag,
    }));
  }, [category, debouncedSearch, sort, filter, tag, dispatch]);

  const handlePageChange = (p: number) => {
    dispatch(fetchPosts({
      category, search: debouncedSearch, page: p, sort, filter, tag,
    }));
  };

  const emptyTitle = posts.length === 0
    ? tag
      ? "No posts with this tag."
      : search
        ? "No search results."
        : filter !== "All Posts"
          ? "No matching posts."
          : "No posts found."
    : "No posts yet.";

  return (
    <Box>
      <CommunityNav />

      {/* Header */}
      <Stack direction="row" alignItems="center"
        justifyContent="space-between" mb={4} flexWrap="wrap" gap={2}>
        <Box>
          <Typography variant="h4" fontWeight={700}>All Posts</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Every post across all community groups.
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} flexWrap="wrap">
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
      </Stack>

      {/* Search + Controls */}
      <Stack direction={{ xs: "column", md: "row" }}
        spacing={2} mb={2} alignItems="center">
        <Box sx={{ flexGrow: 1, maxWidth: 420 }}>
          <SearchBar
            value={search}
            onChange={setSearch}
            placeholder="Search posts, tags, authors..."
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

      {/* Filters */}
      <Stack direction="row" spacing={1} mb={1} flexWrap="wrap" gap={1}>
        {POST_FILTERS.map((f) => (
          <Chip
            key={f}
            label={f}
            clickable
            onClick={() => setFilter(f)}
            color={filter === f ? "primary" : "default"}
            sx={{
              fontWeight: 600,
              fontSize: "0.78rem",
              ...(filter === f && {
                background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                color: "#fff",
              }),
            }}
          />
        ))}
      </Stack>

      {tag && (
        <Stack direction="row" alignItems="center" spacing={1} mb={1}>
          <TagRounded sx={{ fontSize: 18, color: "primary.main" }} />
          <Typography variant="body2" color="text.secondary">
            Filtering by tag:
          </Typography>
          <Chip
            label={tag}
            size="small"
            color="primary"
            onDelete={() => setTag("")}
            sx={{ fontWeight: 600, fontSize: "0.75rem" }}
          />
        </Stack>
      )}

      {/* Categories */}
      <Stack direction="row" spacing={1} mb={3} flexWrap="wrap" gap={1}>
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
          title={emptyTitle}
          description="Try adjusting your search or filters."
          icon={<GroupsRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <Stack spacing={2}>
          {posts.map((post) => (
            <PostCard
              key={post.id}
              post={post}
              onTagClick={(t) => setTag(t)}
            />
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

export default CommunityPage;
