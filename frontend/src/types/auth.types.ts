export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}