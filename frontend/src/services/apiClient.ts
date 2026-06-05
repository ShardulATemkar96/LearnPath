import axios from "axios";
import { tokenUtils } from "../utils/tokenUtils";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api/v1",
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
});

// Request interceptor — attach token
apiClient.interceptors.request.use(
  (config) => {
    const token = tokenUtils.getAccessToken();
    if (token) config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor — handle 401 + refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;

    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      try {
        const accessToken = tokenUtils.getAccessToken();
        const refreshToken = tokenUtils.getRefreshToken();

        const { data } = await axios.post(
          `${import.meta.env.VITE_API_BASE_URL}/auth/refresh`,
          { accessToken, refreshToken }
        );

        tokenUtils.setTokens(data.data.accessToken, data.data.refreshToken);
        original.headers.Authorization = `Bearer ${data.data.accessToken}`;
        return apiClient(original);
      } catch {
        tokenUtils.clearTokens();
        window.location.href = "/login";
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;