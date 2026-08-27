import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Chip, CircularProgress, Divider,
  MenuItem, Select, Skeleton, Snackbar, Stack, Typography,
} from "@mui/material";
import {
  AddRounded, ArrowBackRounded, EditRounded, DeleteRounded,
  GroupsRounded, LockRounded, PersonRounded, PublicRounded,
  PushPinRounded, ArticleRounded,
} from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import {
  fetchGroup, fetchGroupPosts, searchPostsThunk,
  joinGroupThunk, leaveGroupThunk, clearSelectedGroup,
  pinPostThunk, unpinPostThunk, banUserThunk, unbanUserThunk,
  deleteGroupThunk,
} from "../../redux/slices/communitySlice";
import {
  selectSelectedGroup, selectGroupDetailLoading, selectGroupsError,
  selectGroupPosts, selectGroupPostsLoading, selectGroupPostsTotalPages,
  selectPosts, selectCommunityLoading, selectCommunityError, selectTotalPages,
} from "../../redux/selectors/communitySelectors";
import PostCard    from "../../components/community/PostCard/PostCard";
import CreatePostModal from "../../components/community/CreatePostModal/CreatePostModal";
import CreateGroupModal from "../../components/community/CreateGroupModal/CreateGroupModal";
import ConfirmDialog from "../../components/common/ConfirmDialog/ConfirmDialog";
import SearchBar   from "../../components/common/SearchBar/SearchBar";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import EmptyState  from "../../components/common/EmptyState/EmptyState";
import { dateUtils } from "../../utils/dateUtils";
import { useDebounce } from "../../hooks/useDebounce";
import { useAuth } from "../../hooks/useAuth";
import { ROUTES } from "../../constants/routes";

type SortOption = "Newest" | "Top" | "Trending";

const SORT_OPTIONS: SortOption[] = ["Newest", "Top", "Trending"];

const CommunityGroupDetailPage = () => {
  const { groupId } = useParams<{ groupId: string }>();
  const dispatch   = useDispatch<AppDispatch>();
  const navigate   = useNavigate();
  const { isAdmin } = useAuth();

  const group        = useSelector(selectSelectedGroup);
  const groupLoading = useSelector(selectGroupDetailLoading);
  const groupsError  = useSelector(selectGroupsError);

  const groupPosts          = useSelector(selectGroupPosts);
  const groupPostsLoading   = useSelector(selectGroupPostsLoading);
  const groupPostsTotalPages = useSelector(selectGroupPostsTotalPages);
  const trendingPosts       = useSelector(selectPosts);
  const trendingLoading     = useSelector(selectCommunityLoading);
  const trendingTotalPages  = useSelector(selectTotalPages);
  const trendingError       = useSelector(selectCommunityError);

  const [sort,      setSort]     = useState<SortOption>("Newest");
  const [search,    setSearch]   = useState("");
  const [page,      setPage]     = useState(1);
  const [joinLoading, setJoinLoading] = useState(false);
  const [actionMsg, setActionMsg]     = useState("");
  const [actionError, setActionError] = useState("");
  const [createOpen, setCreateOpen]   = useState(false);
  const [editOpen, setEditOpen]       = useState(false);
  const [deleteOpen, setDeleteOpen]   = useState(false);
  const [memberLoading, setMemberLoading] = useState("");

  const debouncedSearch = useDebounce(search, 400);
  const isTrending = sort === "Trending";

  const posts        = isTrending ? trendingPosts : groupPosts;
  const postsLoading = isTrending ? trendingLoading : groupPostsLoading;
  const totalPages   = isTrending ? trendingTotalPages : groupPostsTotalPages;
  const postsError   = isTrending ? trendingError : groupsError;

  useEffect(() => {
    if (groupId) {
      const promise = dispatch(fetchGroup(Number(groupId)));
      return () => {
        (promise as any).abort?.();
        dispatch(clearSelectedGroup());
      };
    }
    return () => { dispatch(clearSelectedGroup()); };
  }, [groupId, dispatch]);

  useEffect(() => {
    setSearch("");
    setSort("Newest");
    setPage(1);
  }, [groupId]);

  useEffect(() => {
    if (!groupId) return;
    const gid = Number(groupId);
    let promise: any;
    if (isTrending) {
      promise = dispatch(searchPostsThunk({ groupId: gid, search: debouncedSearch, page }));
    } else {
      promise = dispatch(fetchGroupPosts({
        groupId: gid,
        search: debouncedSearch,
        sort: sort === "Newest" ? "Newest" : "Score",
        page,
      }));
    }
    return () => {
      promise?.abort?.();
    };
  }, [groupId, isTrending, sort, debouncedSearch, page, dispatch]);

  const handleSortChange = (value: SortOption) => {
    setSort(value);
    setPage(1);
  };

  const handleJoin = async () => {
    if (!groupId) return;
    setJoinLoading(true); setActionError("");
    const result = await dispatch(joinGroupThunk(Number(groupId)));
    setJoinLoading(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setActionMsg("Joined group.");
    } else {
      setActionError((result as any).payload ?? "Failed to join group.");
    }
  };

  const handleLeave = async () => {
    if (!groupId) return;
    setJoinLoading(true); setActionError("");
    const result = await dispatch(leaveGroupThunk(Number(groupId)));
    setJoinLoading(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setActionMsg("Left group.");
    } else {
      setActionError((result as any).payload ?? "Failed to leave group.");
    }
  };

  const handlePinToggle = async (post: { id: number; isPinned: boolean }) => {
    setActionError("");
    const result = await dispatch(
      post.isPinned ? unpinPostThunk(post.id) : pinPostThunk(post.id)
    );
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setActionMsg(post.isPinned ? "Post unpinned." : "Post pinned.");
    } else {
      setActionError((result as any).payload ?? "Failed to update pin.");
    }
  };

  const handleBanToggle = async (userId: string, isBanned: boolean) => {
    if (!groupId) return;
    setActionError(""); setMemberLoading(userId);
    const result = await dispatch(
      isBanned
        ? unbanUserThunk({ groupId: Number(groupId), userId })
        : banUserThunk({ groupId: Number(groupId), userId })
    );
    setMemberLoading("");
    if ((result as any).meta?.requestStatus === "fulfilled") {
      setActionMsg(isBanned ? "Member unbanned." : "Member banned.");
    } else {
      setActionError((result as any).payload ?? "Failed to update member.");
    }
  };

  const handleDeleteGroup = async () => {
    if (!groupId) return;
    setActionError("");
    const result = await dispatch(deleteGroupThunk(Number(groupId)));
    setDeleteOpen(false);
    if ((result as any).meta?.requestStatus === "fulfilled") {
      navigate(ROUTES.COMMUNITY_GROUPS);
    } else {
      setActionError((result as any).payload ?? "Failed to delete group.");
    }
  };

  const role     = group?.currentUserRole;
  const isMember = !!role;
  const canLeave = isMember && role !== "Owner";
  const canPin   = isAdmin || role === "Owner";
  const canManage = isAdmin || role === "Owner";

  if (groupLoading) return (
    <Box>
      <Skeleton height={40} width={180} sx={{ mb: 2 }} />
      <Skeleton variant="rounded" height={160} sx={{ borderRadius: 4, mb: 3 }} />
      <Skeleton variant="rounded" height={120} sx={{ borderRadius: 3, mb: 2 }} />
      <Skeleton variant="rounded" height={120} sx={{ borderRadius: 3 }} />
    </Box>
  );

  if (!group) return null;

  const pinnedPosts = posts.filter((p) => p.isPinned);
  const normalPosts = posts.filter((p) => !p.isPinned);

  return (
    <Box>
      <Button
        startIcon={<ArrowBackRounded />}
        onClick={() => navigate(ROUTES.COMMUNITY_GROUPS)}
        sx={{ mb: 3, color: "text.secondary" }}
      >
        Groups
      </Button>

      {/* Group info */}
      <Box sx={{
        p: { xs: 3, md: 4 }, borderRadius: 4, mb: 4,
        bgcolor: "background.paper",
        boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
        border: "1.5px solid", borderColor: "divider",
      }}>
        <Stack spacing={2.5}>
          <Stack direction="row" alignItems="flex-start"
            justifyContent="space-between" flexWrap="wrap" gap={2}>
            <Stack direction="row" alignItems="center" spacing={1.5}>
              <Box sx={{
                width: 48, height: 48, borderRadius: 2.5, flexShrink: 0,
                display: "flex", alignItems: "center", justifyContent: "center",
                background: "rgba(108,99,255,0.1)",
              }}>
                <GroupsRounded sx={{ color: "primary.main" }} />
              </Box>
              <Box>
                <Typography variant="h4" fontWeight={700} lineHeight={1.3}>
                  {group.name}
                </Typography>
                <Typography variant="body2" color="text.secondary" mt={0.5}>
                  {group.description}
                </Typography>
              </Box>
            </Stack>

            {canManage && (
              <>
                <Button
                  variant="outlined"
                  startIcon={<EditRounded />}
                  onClick={() => setEditOpen(true)}
                  sx={{ borderRadius: 2 }}
                >
                  Edit
                </Button>
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<DeleteRounded />}
                  onClick={() => setDeleteOpen(true)}
                  sx={{ borderRadius: 2 }}
                >
                  Delete
                </Button>
              </>
            )}

            {canLeave ? (
              <Button
                variant="outlined"
                color="error"
                onClick={handleLeave}
                disabled={joinLoading}
                sx={{ borderRadius: 2 }}
              >
                {joinLoading
                  ? <CircularProgress size={20} />
                  : "Leave Group"}
              </Button>
            ) : isMember ? (
              <Chip
                label="Owner"
                size="small"
                color="primary"
                sx={{ fontWeight: 600 }}
              />
            ) : (
              <Button
                variant="contained"
                onClick={handleJoin}
                disabled={joinLoading}
                sx={{
                  borderRadius: 2,
                  background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
                }}
              >
                {joinLoading
                  ? <CircularProgress size={20} sx={{ color: "#fff" }} />
                  : "Join Group"}
              </Button>
            )}
          </Stack>

          <Divider />

          <Stack direction="row" alignItems="center"
            spacing={2} flexWrap="wrap" gap={1}>
            <Chip
              icon={group.isPublic
                ? <PublicRounded sx={{ fontSize: 15 }} />
                : <LockRounded sx={{ fontSize: 15 }} />}
              label={group.isPublic ? "Public" : "Private"}
              size="small"
              sx={{ fontWeight: 600, fontSize: "0.72rem" }}
            />
            <Stack direction="row" alignItems="center" spacing={0.5}>
              <GroupsRounded sx={{ fontSize: 15, color: "text.secondary" }} />
              <Typography variant="caption" color="text.secondary">
                {group.memberCount} members
              </Typography>
            </Stack>
            <Stack direction="row" alignItems="center" spacing={0.5}>
              <ArticleRounded sx={{ fontSize: 15, color: "text.secondary" }} />
              <Typography variant="caption" color="text.secondary">
                {group.postCount} posts
              </Typography>
            </Stack>
            <Stack direction="row" alignItems="center" spacing={0.5}>
              <PersonRounded sx={{ fontSize: 15, color: "text.secondary" }} />
              <Typography variant="caption" color="text.secondary">
                Created by {group.ownerName}
              </Typography>
            </Stack>
            <Typography variant="caption" color="text.secondary">
              · {dateUtils.format(group.createdAt)}
            </Typography>
          </Stack>
        </Stack>
      </Box>

      {actionError && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>
          {actionError}
        </Alert>
      )}

      {canManage && group.members.length > 0 && (
        <Box sx={{
          p: { xs: 3, md: 4 }, borderRadius: 4, mb: 4,
          bgcolor: "background.paper",
          boxShadow: "0 4px 24px rgba(0,0,0,0.07)",
          border: "1.5px solid", borderColor: "divider",
        }}>
          <Typography variant="h6" fontWeight={700} mb={2}>
            Members ({group.members.length})
          </Typography>
          <Stack spacing={1.5}>
            {group.members.map((member) => {
              const isBanned = member.role === "Banned";
              const isOwner  = member.role === "Owner";
              return (
                <Stack
                  key={member.userId}
                  direction="row"
                  alignItems="center"
                  justifyContent="space-between"
                  flexWrap="wrap"
                  gap={1}
                  sx={{
                    p: 1.5,
                    borderRadius: 2,
                    bgcolor: isBanned ? "action.hover" : "transparent",
                    border: "1px solid", borderColor: "divider",
                  }}
                >
                  <Stack direction="row" alignItems="center" spacing={1}>
                    <PersonRounded sx={{ fontSize: 20, color: "text.secondary" }} />
                    <Box>
                      <Typography variant="body2" fontWeight={600}>
                        {member.userName}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Joined {dateUtils.format(member.joinedAt)}
                      </Typography>
                    </Box>
                  </Stack>
                  {isOwner ? (
                    <Chip label="Owner" size="small" color="primary"
                      sx={{ fontWeight: 600 }} />
                  ) : isBanned ? (
                    <Button
                      variant="outlined"
                      color="primary"
                      size="small"
                      disabled={memberLoading === member.userId}
                      onClick={() => handleBanToggle(member.userId, true)}
                      sx={{ borderRadius: 2 }}
                    >
                      {memberLoading === member.userId
                        ? <CircularProgress size={16} />
                        : "Unban"}
                    </Button>
                  ) : (
                    <Button
                      variant="outlined"
                      color="error"
                      size="small"
                      disabled={memberLoading === member.userId}
                      onClick={() => handleBanToggle(member.userId, false)}
                      sx={{ borderRadius: 2 }}
                    >
                      {memberLoading === member.userId
                        ? <CircularProgress size={16} />
                        : "Ban"}
                    </Button>
                  )}
                </Stack>
              );
            })}
          </Stack>
        </Box>
      )}

      {postsError && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>
          {postsError}
        </Alert>
      )}

      {/* Search + Sort */}
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
        <Select
          value={sort}
          size="small"
          onChange={(e) => handleSortChange(e.target.value as SortOption)}
          sx={{ minWidth: 140, borderRadius: 2 }}
        >
          {SORT_OPTIONS.map((s) => (
            <MenuItem key={s} value={s}>{s}</MenuItem>
          ))}
        </Select>
        <Button
          variant="contained"
          startIcon={<AddRounded />}
          onClick={() => setCreateOpen(true)}
          sx={{
            borderRadius: 2,
            background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
          }}
        >
          New Post
        </Button>
      </Stack>

      {/* Pinned announcement */}
      {pinnedPosts.length > 0 && (
        <Box sx={{ mb: 4 }}>
          <Stack direction="row" alignItems="center" spacing={1} mb={2}>
            <PushPinRounded sx={{ fontSize: 18, color: "primary.main" }} />
            <Typography variant="h6" fontWeight={700}>
              Pinned Announcement
            </Typography>
          </Stack>
          <Stack spacing={2}>
            {pinnedPosts.map((post) => (
              <PostCard
                key={post.id}
                post={post}
                canPin={canPin}
                onPinToggle={handlePinToggle}
              />
            ))}
          </Stack>
        </Box>
      )}

      {/* Posts */}
      <Typography variant="h6" fontWeight={700} mb={2}>
        Posts ({posts.length})
      </Typography>

      {postsLoading ? (
        <Stack spacing={2}>
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} variant="rounded"
              height={140} sx={{ borderRadius: 3 }} />
          ))}
        </Stack>
      ) : normalPosts.length === 0 ? (
        <EmptyState
          title="No posts yet."
          description="Be the first to start a discussion in this group!"
          icon={<GroupsRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <Stack spacing={2}>
          {normalPosts.map((post) => (
            <PostCard
              key={post.id}
              post={post}
              canPin={canPin}
              onPinToggle={handlePinToggle}
            />
          ))}
        </Stack>
      )}

      <PaginationBar
        page={page}
        totalPages={totalPages}
        onChange={setPage}
      />

      <CreatePostModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        groupId={groupId ? Number(groupId) : undefined}
        onSaved={() => {
          setActionMsg("Post published.");
          setSort("Newest");
          setPage(1);
        }}
      />

      <CreateGroupModal
        open={editOpen}
        onClose={() => setEditOpen(false)}
        group={group}
        onSaved={() => {
          setActionMsg("Group updated.");
          if (groupId) dispatch(fetchGroup(Number(groupId)));
        }}
      />

      <ConfirmDialog
        open={deleteOpen}
        title="Delete group?"
        message={`Are you sure you want to delete "${group.name}"? This cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={handleDeleteGroup}
        onCancel={() => setDeleteOpen(false)}
      />

      <Snackbar
        open={!!actionMsg}
        autoHideDuration={3000}
        onClose={() => setActionMsg("")}
        message={actionMsg}
      />
    </Box>
  );
};

export default CommunityGroupDetailPage;
