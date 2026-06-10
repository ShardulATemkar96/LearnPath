import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import apiClient from "../../services/apiClient";

interface PathProgress {
  id: number;
  title: string;
  completedModules: number;
  totalModules: number;
}

interface ActivityItem {
  id: number;
  type: "completion" | "achievement" | "join";
  message: string;
  time: string;
}

interface DashboardStats {
  enrolledPaths: number;
  completedModules: number;
  activeClassrooms: number;
  certificates: number;
}

interface DashboardState {
  stats: DashboardStats | null;
  pathProgress: PathProgress[];
  recentActivity: ActivityItem[];
  loading: boolean;
  error: string | null;
}

const initialState: DashboardState = {
  stats: null,
  pathProgress: [],
  recentActivity: [],
  loading: false,
  error: null,
};

export const fetchDashboardData = createAsyncThunk(
  "dashboard/fetchAll",
  async (_, { rejectWithValue }) => {
    try {
      const { data } = await apiClient.get("/dashboard");
      return data.data;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load dashboard.");
    }
  }
);

const dashboardSlice = createSlice({
  name: "dashboard",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder.addCase(fetchDashboardData.pending, (state) => {
      state.loading = true;
      state.error = null;
    });
    builder.addCase(fetchDashboardData.fulfilled, (state, action) => {
      state.loading = false;
      state.stats = action.payload.stats;
      state.pathProgress = action.payload.pathProgress;
      state.recentActivity = action.payload.recentActivity;
    });
    builder.addCase(fetchDashboardData.rejected, (state, action) => {
      state.loading = false;
      state.error = action.payload as string;
    });
  },
});

export default dashboardSlice.reducer;
