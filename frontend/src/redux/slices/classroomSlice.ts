import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { classroomService } from "../../services/classroomService";
import {
  Classroom, ClassroomDetail,
  CreateClassroomRequest, CreateAssignmentRequest,
} from "../../types/classroom.types";

interface ClassroomState {
  classrooms: Classroom[];
  selectedClassroom: ClassroomDetail | null;
  loading: boolean;
  detailLoading: boolean;
  error: string | null;
}

const initialState: ClassroomState = {
  classrooms: [],
  selectedClassroom: null,
  loading: false,
  detailLoading: false,
  error: null,
};

export const fetchMyClassrooms = createAsyncThunk(
  "classrooms/fetchMy",
  async (_, { rejectWithValue }) => {
    try { return await classroomService.getMy(); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed."); }
  }
);

export const fetchClassroomById = createAsyncThunk(
  "classrooms/fetchById",
  async (id: number, { rejectWithValue }) => {
    try { return await classroomService.getById(id); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed."); }
  }
);

export const createClassroomThunk = createAsyncThunk(
  "classrooms/create",
  async (payload: CreateClassroomRequest, { rejectWithValue }) => {
    try { return await classroomService.create(payload); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed."); }
  }
);

export const joinClassroomThunk = createAsyncThunk(
  "classrooms/join",
  async (inviteCode: string, { rejectWithValue }) => {
    try { await classroomService.join(inviteCode); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed."); }
  }
);

export const deleteClassroomThunk = createAsyncThunk(
  "classrooms/delete",
  async (id: number, { rejectWithValue }) => {
    try { await classroomService.delete(id); return id; }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed."); }
  }
);

export const createAssignmentThunk = createAsyncThunk(
  "classrooms/createAssignment",
  async (
    { classroomId, payload }: { classroomId: number; payload: CreateAssignmentRequest },
    { rejectWithValue }
  ) => {
    try { return await classroomService.createAssignment(classroomId, payload); }
    catch (err: any) { return rejectWithValue(err.response?.data?.message ?? "Failed."); }
  }
);

const classroomSlice = createSlice({
  name: "classrooms",
  initialState,
  reducers: {
    clearSelectedClassroom(state) { state.selectedClassroom = null; },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMyClassrooms.pending, (s) => { s.loading = true; s.error = null; })
      .addCase(fetchMyClassrooms.fulfilled, (s, a) => { s.loading = false; s.classrooms = a.payload; })
      .addCase(fetchMyClassrooms.rejected, (s, a) => { s.loading = false; s.error = a.payload as string; });

    builder
      .addCase(fetchClassroomById.pending, (s) => { s.detailLoading = true; s.error = null; })
      .addCase(fetchClassroomById.fulfilled, (s, a) => { s.detailLoading = false; s.selectedClassroom = a.payload; })
      .addCase(fetchClassroomById.rejected, (s, a) => { s.detailLoading = false; s.error = a.payload as string; });

    builder
      .addCase(createClassroomThunk.fulfilled, (s, a) => { s.classrooms.unshift(a.payload); });

    builder
      .addCase(deleteClassroomThunk.fulfilled, (s, a) => {
        s.classrooms = s.classrooms.filter((c) => c.id !== a.payload);
      });

    builder
      .addCase(createAssignmentThunk.fulfilled, (s, a) => {
        if (s.selectedClassroom && a.payload)
          s.selectedClassroom.assignments.unshift(a.payload);
      });
  },
});

export const { clearSelectedClassroom } = classroomSlice.actions;
export default classroomSlice.reducer;