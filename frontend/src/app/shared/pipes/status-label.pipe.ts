import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'statusLabel',
  standalone: true
})
export class StatusLabelPipe implements PipeTransform {
  transform(status: string): string {
    const labels: Record<string, string> = {
      'Submitted': 'Submitted',
      'UnderReview': 'Under Review',
      'Shortlisted': 'Shortlisted',
      'Accepted': 'Accepted',
      'Rejected': 'Rejected',
      'Withdrawn': 'Withdrawn',
      'Draft': 'Draft',
      'Active': 'Active',
      'Closed': 'Closed',
      'Published': 'Published'
    };
    return labels[status] ?? status;
  }
}
