import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Alert, Box, Button, Grid, Skeleton, Stack, Typography,
} from "@mui/material";
import { AddRounded, GroupsRounded } from "@mui/icons-material";
import { AppDispatch } from "../../redux/store";
import { fetchGroups, clearGroupsError } from "../../redux/slices/communitySlice";
import {
  selectGroups, selectGroupsLoading, selectGroupsError,
  selectGroupsTotalPages, selectGroupsCurrentPage,
} from "../../redux/selectors/communitySelectors";
import GroupCard    from "../../components/community/GroupCard/GroupCard";
import CreateGroupModal from "../../components/community/CreateGroupModal/CreateGroupModal";
import SearchBar    from "../../components/common/SearchBar/SearchBar";
import PaginationBar from "../../components/common/PaginationBar/PaginationBar";
import EmptyState   from "../../components/common/EmptyState/EmptyState";
import CommunityNav from "../../components/community/CommunityNav/CommunityNav";
import { useDebounce } from "../../hooks/useDebounce";
import { useAuth } from "../../hooks/useAuth";

const CommunityGroupsPage = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { isAdmin } = useAuth();

  const groups     = useSelector(selectGroups);
  const loading    = useSelector(selectGroupsLoading);
  const error      = useSelector(selectGroupsError);
  const totalPages = useSelector(selectGroupsTotalPages);
  const page       = useSelector(selectGroupsCurrentPage);

  const [search, setSearch] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const debouncedSearch = useDebounce(search, 400);

  useEffect(() => {
    dispatch(clearGroupsError());
    const promise = dispatch(fetchGroups({ search: debouncedSearch, page: 1 }));
    return () => {
      (promise as any).abort?.();
    };
  }, [debouncedSearch, dispatch]);

  const handlePageChange = (p: number) => {
    dispatch(clearGroupsError());
    dispatch(fetchGroups({ search: debouncedSearch, page: p }));
  };

  return (
    <Box>
      <CommunityNav />

      {/* Header */}
      <Stack direction="row" alignItems="center"
        justifyContent="space-between" mb={4} flexWrap="wrap" gap={2}>
        <Box>
          <Typography variant="h4" fontWeight={700}>Groups</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Join study groups and discuss with peers.
          </Typography>
        </Box>
        {isAdmin && (
          <Button
            variant="contained"
            startIcon={<AddRounded />}
            onClick={() => setCreateOpen(true)}
            sx={{
              borderRadius: 2,
              background: "linear-gradient(135deg, #6C63FF, #9D97FF)",
            }}
          >
            Create Group
          </Button>
        )}
      </Stack>

      {/* Search */}
      <Box sx={{ maxWidth: 420, mb: 3 }}>
        <SearchBar
          value={search}
          onChange={setSearch}
          placeholder="Search groups..."
          fullWidth
        />
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>{error}</Alert>
      )}

      {/* Groups */}
      {loading ? (
        <Grid container spacing={3}>
          {Array.from({ length: 6 }).map((_, i) => (
            <Grid item xs={12} sm={6} lg={4} key={i}>
              <Skeleton variant="rounded" height={230} sx={{ borderRadius: 4 }} />
            </Grid>
          ))}
        </Grid>
      ) : groups.length === 0 ? (
        <EmptyState
          title={search ? "No matching groups." : "No groups yet."}
          description={search
            ? "Try a different search term."
            : "Check back later for new study groups."}
          icon={<GroupsRounded sx={{ fontSize: 52 }} />}
        />
      ) : (
        <Grid container spacing={3}>
          {groups.map((group) => (
            <Grid item xs={12} sm={6} lg={4} key={group.id}>
              <GroupCard group={group} />
            </Grid>
          ))}
        </Grid>
      )}

      <PaginationBar
        page={page}
        totalPages={totalPages}
        onChange={handlePageChange}
      />

      <CreateGroupModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSaved={() => {
          dispatch(clearGroupsError());
          dispatch(fetchGroups({ search: debouncedSearch, page: 1 }));
        }}
      />
    </Box>
  );
};

export default CommunityGroupsPage;
