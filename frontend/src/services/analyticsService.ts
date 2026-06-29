import apiClient from "./apiClient";
import { UserAnalytics } from "../types/analytics.types";

export const analyticsService = {
  getMyAnalytics: async (): Promise<UserAnalytics> => {
    const { data } = await apiClient.get("/analytics");
    return data.data;
  },
};