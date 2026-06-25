import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApplyRequest, ApplyResponse, JobApplication } from '../models/application.model';

export interface ApplicationSummary {
  id: string;
  candidateId: string;
  jobId: string;
  status: string;
  coverLetter: string;
  resumeUrl: string;
  submittedAtUtc: string;
  lastUpdatedAtUtc: string;
  jobTitle?: string;
}

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  apply(req: ApplyRequest): Observable<ApplyResponse> {
    return this.http.post<ApplyResponse>(`${this.apiUrl}/api/applications`, req);
  }

  getApplication(id: string): Observable<JobApplication> {
    return this.http.get<JobApplication>(`${this.apiUrl}/api/applications/${id}`);
  }

  getMyApplications(): Observable<ApplicationSummary[]> {
    return this.http.get<ApplicationSummary[]>(`${this.apiUrl}/api/applications/my`);
  }

  getExistingApplication(jobId: string): ApplicationSummary | null {
    try {
      const apps: ApplicationSummary[] = JSON.parse(localStorage.getItem('tb_my_applications') ?? '[]');
      return apps.find(a => a.jobId === jobId) ?? null;
    } catch { return null; }
  }

  getJobApplications(jobId: string): Observable<ApplicationSummary[]> {
    return this.http.get<ApplicationSummary[]>(`${this.apiUrl}/api/applications?jobId=${jobId}`);
  }

  updateStatus(id: string, newStatus: string, rejectionReason?: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/api/applications/${id}/status`, { newStatus, rejectionReason });
  }

  withdraw(id: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/api/applications/${id}/withdraw`, {});
  }
}
