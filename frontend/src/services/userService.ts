import apiClient from "./apiClient";

export interface UserProfile {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  avatarUrl?: string;
  bio?: string;
  roles: string[];
  createdAt: string;
  totalPathsCreated: number;
  totalModulesCompleted: number;
  totalCertificates: number;
}

export interface UpdateProfilePayload {
  firstName: string;
  lastName: string;
  avatarUrl?: string;
  bio?: string;
}

export const userService = {
  getProfile: async (): Promise<UserProfile> => {
    const { data } = await apiClient.get("/users/me");
    return data.data;
  },

  updateProfile: async (payload: UpdateProfilePayload): Promise<UserProfile> => {
    const { data } = await apiClient.put("/users/me", payload);
    return data.data;
  },

  changePassword: async (
    currentPassword: string, newPassword: string
  ): Promise<void> => {
    await apiClient.put("/users/me/password", { currentPassword, newPassword });
  },
};
