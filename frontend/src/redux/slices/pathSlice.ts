import { createAsyncThunk, createSlice, PayloadAction } from "@reduxjs/toolkit";
import { pathService } from "../../services/pathService";
import {
  LearningPath, LearningPathDetail, CreateLearningPathRequest,
} from "../../types/path.types";

interface PathState {
  publicPaths: LearningPath[];
  myPaths: LearningPath[];
  selectedPath: LearningPathDetail | null;
  loading: boolean;
  detailLoading: boolean;
  error: string | null;
}

const initialState: PathState = {
  publicPaths: [],
  myPaths: [],
  selectedPath: null,
  loading: false,
  detailLoading: false,
  error: null,
};

export const fetchPublicPaths = createAsyncThunk(
  "paths/fetchPublic",
  async (_, { rejectWithValue }) => {
    try { return await pathService.getPublic(); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed to load paths."); }
  }
);

export const fetchMyPaths = createAsyncThunk(
  "paths/fetchMy",
  async (_, { rejectWithValue }) => {
    try { return await pathService.getMyPaths(); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed to load paths."); }
  }
);

export const fetchPathById = createAsyncThunk(
  "paths/fetchById",
  async (id: number, { rejectWithValue }) => {
    try { return await pathService.getById(id); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Path not found."); }
  }
);

export const createPathThunk = createAsyncThunk(
  "paths/create",
  async (payload: CreateLearningPathRequest, { rejectWithValue }) => {
    try { return await pathService.create(payload); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed to create path."); }
  }
);

export const deletePathThunk = createAsyncThunk(
  "paths/delete",
  async (id: number, { rejectWithValue }) => {
    try { await pathService.delete(id); return id; }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed to delete."); }
  }
);

const pathSlice = createSlice({
  name: "paths",
  initialState,
  reducers: {
    clearSelectedPath(state) { state.selectedPath = null; },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPublicPaths.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchPublicPaths.fulfilled, (s, a) => { s.loading = false; s.publicPaths = a.payload; })
      .addCase(fetchPublicPaths.rejected, (s, a) => { s.loading = false; s.error = a.payload as string; });

    builder
      .addCase(fetchMyPaths.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchMyPaths.fulfilled, (s, a) => { s.loading = false; s.myPaths = a.payload; })
      .addCase(fetchMyPaths.rejected, (s, a) => { s.loading = false; s.error = a.payload as string; });

    builder
      .addCase(fetchPathById.pending, (s) => { s.detailLoading = true; s.error = null; })
      .addCase(fetchPathById.fulfilled, (s, a) => { s.detailLoading = false; s.selectedPath = a.payload; })
      .addCase(fetchPathById.rejected, (s, a) => { s.detailLoading = false; s.error = a.payload as string; });

    builder
      .addCase(createPathThunk.fulfilled, (s, a) => { s.myPaths.unshift(a.payload); });

    builder
      .addCase(deletePathThunk.fulfilled, (s, a) => {
        s.myPaths = s.myPaths.filter((p) => p.id !== a.payload);
        s.publicPaths = s.publicPaths.filter((p) => p.id !== a.payload);
      });
  },
});

export const { clearSelectedPath } = pathSlice.actions;
export default pathSlice.reducer;
