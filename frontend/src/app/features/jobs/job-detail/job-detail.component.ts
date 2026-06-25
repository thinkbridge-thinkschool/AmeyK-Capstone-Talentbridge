import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { JobService } from '../../../core/services/job.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ApplicationService, ApplicationSummary } from '../../../core/services/application.service';
import { Job } from '../../../core/models/job.model';
import { UserRole } from '../../../core/models/user.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBadgeComponent],
  templateUrl: './job-detail.component.html'
})
export class JobDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private jobService = inject(JobService);
  private authService = inject(AuthService);
  private applicationService = inject(ApplicationService);

  job: Job | null = null;
  existingApplication: ApplicationSummary | null = null;
  loading = true;
  errorMessage = '';
  actionLoading = false;
  actionSuccess = '';
  actionError = '';

  UserRole = UserRole;

  get isCandidate(): boolean { return this.authService.getRole() === UserRole.Candidate; }
  get isHR(): boolean { return this.authService.getRole() === UserRole.CompanyHR; }
  get isAdmin(): boolean { return this.authService.getRole() === UserRole.Admin; }
  get isLoggedIn(): boolean { return this.authService.isLoggedIn(); }

  get alreadyApplied(): boolean { return !!this.existingApplication; }

  get canApply(): boolean {
    return this.isLoggedIn && this.isCandidate &&
      !this.alreadyApplied &&
      (this.job?.status === 'Active' || this.job?.status === 'Published');
  }

  get canPublish(): boolean {
    return this.isLoggedIn && (this.isHR || this.isAdmin) && this.job?.status === 'Draft';
  }

  get canClose(): boolean {
    return this.isLoggedIn && (this.isHR || this.isAdmin) &&
      (this.job?.status === 'Active' || this.job?.status === 'Published');
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/jobs']);
      return;
    }

    this.jobService.getJob(id).subscribe({
      next: (job) => {
        this.job = job;
        this.loading = false;
        if (this.isCandidate) {
          this.existingApplication = this.applicationService.getExistingApplication(id);
        }
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Job not found or failed to load.';
      }
    });
  }

  publishJob(): void {
    if (!this.job) return;
    this.actionLoading = true;
    this.actionSuccess = '';
    this.actionError = '';

    this.jobService.publishJob(this.job.id).subscribe({
      next: () => {
        this.actionLoading = false;
        this.actionSuccess = 'Job published successfully!';
        if (this.job) this.job = { ...this.job, status: 'Active' };
      },
      error: (err) => {
        this.actionLoading = false;
        this.actionError = err?.error?.message ?? 'Failed to publish job.';
      }
    });
  }

  closeJob(): void {
    if (!this.job) return;
    this.actionLoading = true;
    this.actionSuccess = '';
    this.actionError = '';

    this.jobService.closeJob(this.job.id).subscribe({
      next: () => {
        this.actionLoading = false;
        this.actionSuccess = 'Job closed successfully.';
        if (this.job) this.job = { ...this.job, status: 'Closed' };
      },
      error: (err) => {
        this.actionLoading = false;
        this.actionError = err?.error?.message ?? 'Failed to close job.';
      }
    });
  }

  formatSalary(min: number, max: number): string {
    const fmt = (n: number) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(n);
    return `${fmt(min)} – ${fmt(max)}`;
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
  }
}
