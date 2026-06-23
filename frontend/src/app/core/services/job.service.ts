import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Job, JobSearchResult, PostJobRequest } from '../models/job.model';

@Injectable({ providedIn: 'root' })
export class JobService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  searchJobs(
    keyword: string = '',
    location: string = '',
    page: number = 1,
    size: number = 10
  ): Observable<JobSearchResult> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('size', size.toString());

    if (keyword) params = params.set('keyword', keyword);
    if (location) params = params.set('location', location);

    return this.http.get<JobSearchResult>(`${this.apiUrl}/api/jobs/search`, { params });
  }

  getJob(id: string): Observable<Job> {
    return this.http.get<Job>(`${this.apiUrl}/api/jobs/${id}`);
  }

  postJob(req: PostJobRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/jobs`, req);
  }

  publishJob(id: string, companyId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/jobs/${id}/publish?companyId=${companyId}`, {});
  }

  closeJob(id: string, companyId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/jobs/${id}/close?companyId=${companyId}`, {});
  }
}
