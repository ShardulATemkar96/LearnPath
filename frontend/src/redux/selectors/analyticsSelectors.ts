import { RootState } from "../store";

export const selectAnalyticsData    = (s: RootState) => s.analytics.data;
export const selectAnalyticsLoading = (s: RootState) => s.analytics.loading;
export const selectAnalyticsError   = (s: RootState) => s.analytics.error;
