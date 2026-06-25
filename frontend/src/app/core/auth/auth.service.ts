import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenService } from './token.service';
import { AuthResponse, LoginRequest, RefreshTokenResponse, RegisterRequest, UserRole } from '../models/user.model';

export interface CurrentUser {
  email: string;
  role: string;
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private tokenService = inject(TokenService);
  private router = inject(Router);

  private readonly apiUrl = environment.apiUrl;

  private _currentUser$ = new BehaviorSubject<CurrentUser | null>(this.buildCurrentUser());
  currentUser$ = this._currentUser$.asObservable();

  private buildCurrentUser(): CurrentUser | null {
    if (!this.tokenService.isLoggedIn()) return null;
    const email = this.tokenService.getEmail();
    const role = this.tokenService.getRole();
    const userId = this.tokenService.getUserId();
    if (email === null || role === null || userId === null) return null;
    return { email, role, userId };
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/identity/login`, req).pipe(
      tap(res => {
        this.tokenService.setToken(res.token);
        this.tokenService.setRefreshToken(res.refreshToken);
        this._currentUser$.next(this.buildCurrentUser());
      })
    );
  }

  register(req: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/identity/register`, req);
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    const refreshToken = this.tokenService.getRefreshToken();
    return this.http.post<RefreshTokenResponse>(`${this.apiUrl}/api/identity/refresh`, { refreshToken }).pipe(
      tap(res => {
        this.tokenService.setToken(res.accessToken);
        this.tokenService.setRefreshToken(res.refreshToken);
        this._currentUser$.next(this.buildCurrentUser());
      })
    );
  }

  logout(): void {
    this.tokenService.clearAll();
    this._currentUser$.next(null);
    this.router.navigate(['/auth/login']);
  }

  isLoggedIn(): boolean {
    return this.tokenService.isLoggedIn();
  }

  getRole(): string | null {
    return this.tokenService.getRole();
  }

  getUserId(): string | null {
    return this.tokenService.getUserId();
  }

  isCandidate(): boolean {
    return this.getRole() === UserRole.Candidate;
  }

  isHR(): boolean {
    return this.getRole() === UserRole.CompanyHR;
  }

  isAdmin(): boolean {
    return this.getRole() === UserRole.Admin;
  }
}
