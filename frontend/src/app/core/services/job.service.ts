import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Job, JobSearchResult, PostJobRequest, normalizeJobStatus } from '../models/job.model';

export interface UpdateJobRequest {
  title: string;
  description: string;
  location: string;
  salaryMin: number;
  salaryMax: number;
}

@Injectable({ providedIn: 'root' })
export class JobService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  searchJobs(
    keyword: string = '',
    location: string = '',
    page: number = 1,
    size: number = 10,
    salaryMin?: number,
    salaryMax?: number
  ): Observable<JobSearchResult> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('size', size.toString());

    if (keyword) params = params.set('keyword', keyword);
    if (location) params = params.set('location', location);
    if (salaryMin != null) params = params.set('salaryMin', salaryMin.toString());
    if (salaryMax != null) params = params.set('salaryMax', salaryMax.toString());

    return this.http.get<Job[] | JobSearchResult>(`${this.apiUrl}/api/jobs/search`, { params }).pipe(
      map(resp => {
        if (Array.isArray(resp)) {
          const items = resp.map(j => ({ ...j, status: normalizeJobStatus(j.status) }));
          return { items, totalCount: items.length };
        }
        const items = (resp as JobSearchResult).items.map(j => ({ ...j, status: normalizeJobStatus(j.status) }));
        return { items, totalCount: (resp as JobSearchResult).totalCount };
      })
    );
  }

  getJob(id: string): Observable<Job> {
    return this.http.get<Job>(`${this.apiUrl}/api/jobs/${id}`).pipe(
      map(j => ({ ...j, status: normalizeJobStatus(j.status) }))
    );
  }

  getMyJobs(): Observable<Job[]> {
    return this.http.get<Job[]>(`${this.apiUrl}/api/jobs/mine`).pipe(
      map(jobs => jobs.map(j => ({ ...j, status: normalizeJobStatus(j.status) })))
    );
  }

  postJob(req: PostJobRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/api/jobs`, req);
  }

  updateJob(id: string, req: UpdateJobRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/jobs/${id}`, req);
  }

  deleteJob(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/jobs/${id}`);
  }

  publishJob(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/jobs/${id}/publish`, {});
  }

  closeJob(id: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/api/jobs/${id}/close`, {});
  }
}
