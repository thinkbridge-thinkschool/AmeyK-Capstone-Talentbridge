import { Injectable } from '@angular/core';

const TOKEN_KEY = 'tb_token';

@Injectable({ providedIn: 'root' })
export class TokenService {

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  removeToken(): void {
    localStorage.removeItem(TOKEN_KEY);
  }

  decodeToken(): Record<string, any> | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = parts[1];
      // Pad base64 string to a multiple of 4
      const padded = payload + '='.repeat((4 - payload.length % 4) % 4);
      const decoded = atob(padded);
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }

  getUserId(): string | null {
    const decoded = this.decodeToken();
    if (!decoded) return null;
    // Try common JWT sub claim keys
    return decoded['sub']
      || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
      || decoded['nameid']
      || null;
  }

  getRole(): number | null {
    const decoded = this.decodeToken();
    if (!decoded) return null;
    const roleVal = decoded['role']
      ?? decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      ?? decoded['roles']
      ?? null;
    if (roleVal === null || roleVal === undefined) return null;
    return Number(roleVal);
  }

  getEmail(): string | null {
    const decoded = this.decodeToken();
    if (!decoded) return null;
    return decoded['email']
      || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
      || decoded['unique_name']
      || null;
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    const decoded = this.decodeToken();
    if (!decoded) return false;
    const exp = decoded['exp'];
    if (!exp) return true;
    return Date.now() < exp * 1000;
  }
}
