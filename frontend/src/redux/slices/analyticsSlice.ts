import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { analyticsService } from "../../services/analyticsService";
import { UserAnalytics } from "../../types/analytics.types";

interface AnalyticsState {
  data: UserAnalytics | null;
  loading: boolean;
  error: string | null;
}

const initialState: AnalyticsState = {
  data: null, loading: false, error: null,
};

export const fetchAnalytics = createAsyncThunk(
  "analytics/fetchMy",
  async (_, { rejectWithValue }) => {
    try { return await analyticsService.getMyAnalytics(); }
    catch (err: any) {
      return rejectWithValue(err.response?.data?.message ?? "Failed to load analytics.");
    }
  }
);

const analyticsSlice = createSlice({
  name: "analytics",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchAnalytics.pending,   (s) => { s.loading = true;  s.error = null; })
      .addCase(fetchAnalytics.fulfilled, (s, a) => { s.loading = false; s.data = a.payload; })
      .addCase(fetchAnalytics.rejected,  (s, a) => { s.loading = false; s.error = a.payload as string; });
  },
});

export default analyticsSlice.reducer;