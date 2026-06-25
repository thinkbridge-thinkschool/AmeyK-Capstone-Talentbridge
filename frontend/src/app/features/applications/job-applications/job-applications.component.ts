import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ApplicationService, ApplicationSummary } from '../../../core/services/application.service';
import { JobService } from '../../../core/services/job.service';
import { Job } from '../../../core/models/job.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-job-applications',
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBadgeComponent],
  templateUrl: './job-applications.component.html'
})
export class JobApplicationsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private applicationService = inject(ApplicationService);
  private jobService = inject(JobService);
  private toast = inject(ToastService);

  jobId = '';
  job: Job | null = null;
  applications: ApplicationSummary[] = [];
  loading = true;
  actionLoadingId: string | null = null;

  readonly statusFlow: Record<string, { label: string; next: string; cls: string }[]> = {
    Submitted:   [{ label: 'Start Review', next: 'UnderReview', cls: 'bg-yellow-500 hover:bg-yellow-600 text-white' }],
    UnderReview: [
      { label: 'Shortlist', next: 'Shortlisted', cls: 'bg-purple-600 hover:bg-purple-700 text-white' },
      { label: 'Reject',    next: 'Rejected',    cls: 'bg-red-500 hover:bg-red-600 text-white' }
    ],
    Shortlisted: [
      { label: 'Accept', next: 'Accepted', cls: 'bg-green-600 hover:bg-green-700 text-white' },
      { label: 'Reject', next: 'Rejected', cls: 'bg-red-500 hover:bg-red-600 text-white' }
    ]
  };

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.jobId) { this.router.navigate(['/jobs']); return; }

    this.jobService.getJob(this.jobId).subscribe({
      next: job => { this.job = job; },
      error: () => {}
    });

    this.loadApplications();
  }

  loadApplications(): void {
    this.loading = true;
    this.applicationService.getJobApplications(this.jobId).subscribe({
      next: apps => { this.applications = apps; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Failed to load applications.'); }
    });
  }

  actionsFor(status: string): { label: string; next: string; cls: string }[] {
    return this.statusFlow[status] ?? [];
  }

  updateStatus(appId: string, newStatus: string): void {
    this.actionLoadingId = appId;
    this.applicationService.updateStatus(appId, newStatus).subscribe({
      next: () => {
        this.actionLoadingId = null;
        const app = this.applications.find(a => a.id === appId);
        if (app) app.status = newStatus;
        this.toast.success(`Application moved to ${newStatus}.`);
      },
      error: err => {
        this.actionLoadingId = null;
        this.toast.error(err?.error?.message ?? 'Failed to update status.');
      }
    });
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  get counts(): Record<string, number> {
    return this.applications.reduce((acc, a) => {
      acc[a.status] = (acc[a.status] ?? 0) + 1;
      return acc;
    }, {} as Record<string, number>);
  }
}
