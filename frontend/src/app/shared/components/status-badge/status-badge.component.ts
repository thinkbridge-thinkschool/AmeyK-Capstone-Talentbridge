import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StatusLabelPipe } from '../../pipes/status-label.pipe';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, StatusLabelPipe],
  templateUrl: './status-badge.component.html'
})
export class StatusBadgeComponent {
  @Input() status: string = '';

  get badgeClass(): string {
    const map: Record<string, string> = {
      'Submitted':   'bg-blue-100 text-blue-800',
      'UnderReview': 'bg-yellow-100 text-yellow-800',
      'Shortlisted': 'bg-purple-100 text-purple-800',
      'Accepted':    'bg-green-100 text-green-800',
      'Rejected':    'bg-red-100 text-red-800',
      'Withdrawn':   'bg-gray-100 text-gray-600',
      'Draft':       'bg-gray-100 text-gray-600',
      'Active':      'bg-green-100 text-green-800',
      'Published':   'bg-green-100 text-green-800',
      'Closed':      'bg-red-100 text-red-800'
    };
    return map[this.status] ?? 'bg-gray-100 text-gray-600';
  }
}
