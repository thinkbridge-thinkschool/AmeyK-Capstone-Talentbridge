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
  email: string;
  role: number;
}

export enum UserRole {
  Candidate = 0,
  CompanyHR = 1,
  Admin = 2
}
