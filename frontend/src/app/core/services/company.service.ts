import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Company {
  id: string;
  name: string;
  description: string;
  website?: string;
  isApproved: boolean;
  createdAtUtc: string;
}

export interface CreateCompanyRequest {
  name: string;
  description: string;
  website?: string;
}

@Injectable({ providedIn: 'root' })
export class CompanyService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getMyCompanies(): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.apiUrl}/api/companies/mine`);
  }

  createCompany(req: CreateCompanyRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.apiUrl}/api/companies`, req);
  }

  getAllCompanies(): Observable<Company[]> {
    return this.http.get<Company[]>(`${this.apiUrl}/api/admin/companies`);
  }

  approveCompany(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/api/admin/companies/${id}/approve`, {});
  }
}
