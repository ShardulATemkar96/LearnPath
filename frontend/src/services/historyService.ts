import apiClient from "./apiClient";
import {
  AdminHistoryListResponse, AdminHistoryQuery,
  HistoryActionOption, HistoryListResponse, HistoryQuery,
} from "../types/history.types";

export const historyService = {
  /** Returns only the signed-in user's own history — the API takes no user id. */
  getMy: async (query: HistoryQuery): Promise<HistoryListResponse> => {
    const { data } = await apiClient.get("/history/me", { params: query });
    return data.data;
  },

  getActionTypes: async (): Promise<HistoryActionOption[]> => {
    const { data } = await apiClient.get("/history/action-types");
    return data.data;
  },

  /** Super Admin only. Returns 403 for every other caller. */
  getAdminAccess: async (): Promise<boolean> => {
    const { data } = await apiClient.get("/history/admin/access");
    return data.data.canAccess;
  },

  getAdmin: async (query: AdminHistoryQuery): Promise<AdminHistoryListResponse> => {
    const { data } = await apiClient.get("/history/admin", { params: query });
    return data.data;
  },

  getEntityTypes: async (): Promise<string[]> => {
    const { data } = await apiClient.get("/history/admin/entity-types");
    return data.data;
  },
};
