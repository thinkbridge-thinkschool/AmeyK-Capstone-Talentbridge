import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Notification } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.apiUrl}/api/notifications`);
  }

  markRead(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/api/notifications/${id}/read`, {});
  }

  markAllRead(): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/api/notifications/read-all`, {});
  }
}
