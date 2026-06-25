import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { Notification } from '../../core/models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notifications.component.html'
})
export class NotificationsComponent implements OnInit {
  private notificationService = inject(NotificationService);

  notifications: Notification[] = [];
  loading = true;
  error = '';
  markingAll = false;

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  ngOnInit(): void {
    this.notificationService.getNotifications().subscribe({
      next: n => { this.notifications = n; this.loading = false; },
      error: () => { this.error = 'Failed to load notifications.'; this.loading = false; }
    });
  }

  markRead(id: string): void {
    this.notificationService.markRead(id).subscribe({
      next: () => {
        const n = this.notifications.find(n => n.id === id);
        if (n) n.isRead = true;
      }
    });
  }

  markAllRead(): void {
    this.markingAll = true;
    this.notificationService.markAllRead().subscribe({
      next: () => { this.notifications.forEach(n => n.isRead = true); this.markingAll = false; },
      error: () => { this.markingAll = false; }
    });
  }

  iconBg(type: string): string {
    switch (type) {
      case 'submitted': return 'bg-blue-100 text-blue-600';
      case 'shortlisted': return 'bg-yellow-100 text-yellow-600';
      case 'accepted': return 'bg-green-100 text-green-600';
      case 'rejected': return 'bg-red-100 text-red-500';
      case 'review': return 'bg-purple-100 text-purple-600';
      default: return 'bg-gray-100 text-gray-500';
    }
  }

  iconFor(message: string): 'submitted' | 'shortlisted' | 'accepted' | 'rejected' | 'review' | 'info' {
    const m = message.toLowerCase();
    if (m.includes('submitted') || m.includes('applied')) return 'submitted';
    if (m.includes('shortlist')) return 'shortlisted';
    if (m.includes('accepted')) return 'accepted';
    if (m.includes('rejected') || m.includes('not selected')) return 'rejected';
    if (m.includes('review')) return 'review';
    return 'info';
  }

  formatDate(d: string): string {
    const date = new Date(d);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
