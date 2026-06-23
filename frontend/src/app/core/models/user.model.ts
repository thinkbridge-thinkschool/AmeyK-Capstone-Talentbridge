export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  userRole: string;
}

export enum UserRole {
  Candidate = 'Candidate',
  CompanyHR = 'CompanyHR',
  Admin = 'Admin'
}
