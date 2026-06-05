import { AuthResponse, LoginRequest, RefreshTokenRequest, RegisterRequest } from "../types/auth.types";
import apiClient from "./apiClient";

export const authService = {
  login: async (payload: LoginRequest): Promise<AuthResponse> => {
    const { data } = await apiClient.post("/auth/login", payload);
    return data.data;
  },

  register: async (payload: RegisterRequest): Promise<AuthResponse> => {
    const { data } = await apiClient.post("/auth/register", payload);
    return data.data;
  },

  refresh: async (payload: RefreshTokenRequest): Promise<AuthResponse> => {
    const { data } = await apiClient.post("/auth/refresh", payload);
    return data.data;
  },

  logout: async (): Promise<void> => {
    await apiClient.post("/auth/revoke");
  },
};