import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../../../core/auth/auth.service';
import { JobService } from '../../../core/services/job.service';
import { Job } from '../../../core/models/job.model';

@Component({
  selector: 'app-hr-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './hr-dashboard.component.html'
})
export class HrDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private jobService = inject(JobService);

  currentUser: CurrentUser | null = null;
  myJobs: Job[] = [];
  loadingJobs = true;

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    this.jobService.getMyJobs().subscribe({
      next: jobs => { this.myJobs = jobs; this.loadingJobs = false; },
      error: () => {
        const drafts = JSON.parse(localStorage.getItem('tb_draft_jobs') ?? '[]');
        this.myJobs = drafts;
        this.loadingJobs = false;
      }
    });
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'bg-gray-100 text-gray-600',
      Active: 'bg-green-100 text-green-700',
      Closed: 'bg-red-100 text-red-600',
      Expired: 'bg-orange-100 text-orange-600'
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
