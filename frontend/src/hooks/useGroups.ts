import { useDispatch, useSelector } from "react-redux";
import { AppDispatch } from "../redux/store";
import {
  selectGroups, selectSelectedGroup, selectGroupsLoading,
  selectGroupDetailLoading, selectGroupsError,
  selectGroupsTotalPages, selectGroupsCurrentPage, selectGroupsTotalCount,
  selectGroupPosts, selectGroupPostsLoading,
  selectGroupPostsTotalPages, selectGroupPostsCurrentPage, selectGroupPostsTotalCount,
} from "../redux/selectors/communitySelectors";
import {
  fetchGroups, searchGroupsThunk, fetchGroup,
  createGroupThunk, updateGroupThunk, deleteGroupThunk,
  joinGroupThunk, leaveGroupThunk,
  fetchGroupPosts, createGroupPostThunk,
  clearSelectedGroup, clearGroupPosts,
} from "../redux/slices/communitySlice";
import {
  CreateGroupRequest, UpdateGroupRequest, CreatePostRequest, PostSortOrder,
} from "../types/community.types";

export const useGroups = () => {
  const dispatch = useDispatch<AppDispatch>();

  const groups = useSelector(selectGroups);
  const selectedGroup = useSelector(selectSelectedGroup);
  const groupsLoading = useSelector(selectGroupsLoading);
  const groupDetailLoading = useSelector(selectGroupDetailLoading);
  const groupsError = useSelector(selectGroupsError);
  const groupsTotalPages = useSelector(selectGroupsTotalPages);
  const groupsPage = useSelector(selectGroupsCurrentPage);
  const groupsTotalCount = useSelector(selectGroupsTotalCount);
  const groupPosts = useSelector(selectGroupPosts);
  const groupPostsLoading = useSelector(selectGroupPostsLoading);
  const groupPostsTotalPages = useSelector(selectGroupPostsTotalPages);
  const groupPostsPage = useSelector(selectGroupPostsCurrentPage);
  const groupPostsTotalCount = useSelector(selectGroupPostsTotalCount);

  const loadGroups = (args?: { search?: string; isPublic?: boolean; page?: number }) =>
    dispatch(fetchGroups(args ?? {}));
  const searchGroups = (args?: { search?: string; isPublic?: boolean; page?: number }) =>
    dispatch(searchGroupsThunk(args ?? {}));
  const loadGroup = (groupId: number) => dispatch(fetchGroup(groupId));
  const createGroup = (payload: CreateGroupRequest) => dispatch(createGroupThunk(payload));
  const updateGroup = (groupId: number, payload: UpdateGroupRequest) =>
    dispatch(updateGroupThunk({ groupId, payload }));
  const deleteGroup = (groupId: number) => dispatch(deleteGroupThunk(groupId));
  const joinGroup = (groupId: number) => dispatch(joinGroupThunk(groupId));
  const leaveGroup = (groupId: number) => dispatch(leaveGroupThunk(groupId));
  const loadGroupPosts = (args: {
    groupId: number; search?: string; category?: string; sort?: PostSortOrder; page?: number;
  }) => dispatch(fetchGroupPosts(args));
  const createGroupPost = (groupId: number, payload: CreatePostRequest) =>
    dispatch(createGroupPostThunk({ groupId, payload }));

  return {
    groups, selectedGroup,
    groupsLoading, groupDetailLoading, groupsError,
    groupsTotalPages, groupsPage, groupsTotalCount,
    groupPosts, groupPostsLoading,
    groupPostsTotalPages, groupPostsPage, groupPostsTotalCount,
    loadGroups, searchGroups, loadGroup,
    createGroup, updateGroup, deleteGroup,
    joinGroup, leaveGroup,
    loadGroupPosts, createGroupPost,
    clearSelectedGroup: () => dispatch(clearSelectedGroup()),
    clearGroupPosts: () => dispatch(clearGroupPosts()),
  };
};
