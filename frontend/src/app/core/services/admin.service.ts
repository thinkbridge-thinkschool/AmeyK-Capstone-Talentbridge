import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Job } from '../models/job.model';

export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  role: string;
  isActive: boolean;
  createdAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getAllUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.apiUrl}/api/admin/users`);
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/api/admin/users/${id}/deactivate`, {});
  }

  getAllJobs(): Observable<Job[]> {
    return this.http.get<Job[]>(`${this.apiUrl}/api/admin/jobs`);
  }
}
