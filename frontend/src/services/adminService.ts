import apiClient from "./apiClient";
import { AdminUser, AdminStats } from "../types/admin.types";

export const adminService = {
  getStats: async (): Promise<AdminStats> => {
    const { data } = await apiClient.get("/admin/stats");
    return data.data;
  },

  getUsers: async (search?: string): Promise<AdminUser[]> => {
    const { data } = await apiClient.get("/admin/users", {
      params: search ? { search } : undefined,
    });
    return data.data;
  },

  updateRole: async (userId: string, role: string): Promise<AdminUser> => {
    const { data } = await apiClient.put(`/admin/users/${userId}/role`, { role });
    return data.data;
  },

  toggleStatus: async (userId: string, isActive: boolean): Promise<AdminUser> => {
    const { data } = await apiClient.put(`/admin/users/${userId}/status`, { isActive });
    return data.data;
  },

  deleteUser: async (userId: string): Promise<void> => {
    await apiClient.delete(`/admin/users/${userId}`);
  },
};
