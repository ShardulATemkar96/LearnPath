import { RootState } from "../store";

export const selectDashboardStats    = (state: RootState) => state.dashboard.stats;
export const selectPathProgress      = (state: RootState) => state.dashboard.pathProgress;
export const selectRecentActivity    = (state: RootState) => state.dashboard.recentActivity;
export const selectDashboardLoading  = (state: RootState) => state.dashboard.loading;
export const selectDashboardError    = (state: RootState) => state.dashboard.error;
