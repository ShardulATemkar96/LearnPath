import apiClient from "./apiClient";

export interface AppNotification {
  id: number;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}

export const notificationService = {
  getMy: async (): Promise<AppNotification[]> => {
    const { data } = await apiClient.get("/notifications");
    return data.data;
  },

  markRead: async (id: number): Promise<void> => {
    await apiClient.put(`/notifications/${id}/read`);
  },

  markAllRead: async (): Promise<void> => {
    await apiClient.put("/notifications/read-all");
  },
};
