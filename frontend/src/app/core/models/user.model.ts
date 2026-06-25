export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  role: string;
  fullName?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  userRole: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export enum UserRole {
  Candidate = 'Candidate',
  CompanyHR = 'CompanyHR',
  Admin = 'Admin'
}
