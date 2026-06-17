import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApplyRequest, ApplicationAction, JobApplication } from '../models/application.model';

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  apply(req: ApplyRequest): Observable<JobApplication> {
    return this.http.post<JobApplication>(`${this.apiUrl}/api/applications`, req);
  }

  getApplication(id: string): Observable<JobApplication> {
    return this.http.get<JobApplication>(`${this.apiUrl}/api/applications/${id}`);
  }

  updateStatus(id: string, action: ApplicationAction): Observable<any> {
    return this.http.patch(`${this.apiUrl}/api/applications/${id}/status`, { action });
  }
}
