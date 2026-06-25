import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface HistoryEntry {
  id: string;
  fromStatus: string;
  toStatus: string;
  changedByUserId?: string;
  notes?: string;
  changedAtUtc: string;
}

@Component({
  selector: 'app-status-timeline',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './status-timeline.component.html'
})
export class StatusTimelineComponent implements OnInit {
  @Input({ required: true }) applicationId!: string;

  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  history: HistoryEntry[] = [];
  loading = true;

  ngOnInit(): void {
    this.http.get<HistoryEntry[]>(`${this.apiUrl}/api/applications/${this.applicationId}/history`)
      .subscribe({
        next: h => { this.history = h; this.loading = false; },
        error: () => { this.loading = false; }
      });
  }

  dotColor(status: string): string {
    const map: Record<string, string> = {
      Submitted:   'bg-blue-500',
      UnderReview: 'bg-yellow-500',
      Shortlisted: 'bg-purple-500',
      Accepted:    'bg-green-500',
      Rejected:    'bg-red-500',
      Withdrawn:   'bg-gray-400'
    };
    return map[status] ?? 'bg-gray-400';
  }

  textColor(status: string): string {
    const map: Record<string, string> = {
      Submitted:   'text-blue-700',
      UnderReview: 'text-yellow-700',
      Shortlisted: 'text-purple-700',
      Accepted:    'text-green-700',
      Rejected:    'text-red-700',
      Withdrawn:   'text-gray-500'
    };
    return map[status] ?? 'text-gray-600';
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleString('en-US', {
      month: 'short', day: 'numeric', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
