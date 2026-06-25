import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest } from '@angular/common/http';
import { Observable, map, filter } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UploadProgressEvent {
  progress: number;
  url?: string;
}

@Injectable({ providedIn: 'root' })
export class ResumeService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  upload(file: File): Observable<UploadProgressEvent> {
    const formData = new FormData();
    formData.append('file', file);

    const req = new HttpRequest('POST', `${this.apiUrl}/api/resumes/upload`, formData, {
      reportProgress: true
    });

    return this.http.request(req).pipe(
      filter(event => event.type === HttpEventType.UploadProgress || event.type === HttpEventType.Response),
      map(event => {
        if (event.type === HttpEventType.UploadProgress) {
          const total = event.total ?? 1;
          return { progress: Math.round(100 * event.loaded / total) };
        }
        const body = (event as any).body as { resumeUrl: string };
        return { progress: 100, url: body.resumeUrl };
      })
    );
  }
}
