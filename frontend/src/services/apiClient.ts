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

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) prom.reject(error);
    else prom.resolve(token);
  });
  failedQueue = [];
};

// Response interceptor — handle 401 + refresh (skip login endpoint)
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;

    // Do not intercept 401 on login/register — let the caller handle it
    if (original?.url?.includes("/auth/login") || original?.url?.includes("/auth/register")) {
      return Promise.reject(error);
    }

    // If error is due to abort/cancellation, don't attempt refresh
    if (axios.isCancel(error) || error.code === "ERR_CANCELED" || error.name === "CanceledError") {
      return Promise.reject(error);
    }

    if (error.response?.status === 401 && !original._retry) {
      if (isRefreshing) {
        // Queue this request until the refresh completes
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            original.headers.Authorization = `Bearer ${token}`;
            return apiClient(original);
          })
          .catch((err) => Promise.reject(err));
      }

      original._retry = true;
      isRefreshing = true;
      try {
        const accessToken = tokenUtils.getAccessToken();
        const refreshToken = tokenUtils.getRefreshToken();

        const baseURL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api/v1";
        const { data } = await axios.post(
          `${baseURL}/auth/refresh`,
          { accessToken, refreshToken }
        );

        tokenUtils.setTokens(data.data.accessToken, data.data.refreshToken);
        original.headers.Authorization = `Bearer ${data.data.accessToken}`;
        processQueue(null, data.data.accessToken);
        return apiClient(original);
      } catch (refreshError) {
        processQueue(refreshError, null);
        tokenUtils.clearTokens();
        window.location.href = "/login";
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;