import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ApplicationService, ResumePreview } from '../../../core/services/application.service';
import { AuthService } from '../../../core/auth/auth.service';
import { JobApplication, ApplicationAction } from '../../../core/models/application.model';
import { UserRole } from '../../../core/models/user.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { StatusTimelineComponent } from '../../../shared/components/status-timeline/status-timeline.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBadgeComponent, StatusTimelineComponent],
  templateUrl: './application-detail.component.html'
})
export class ApplicationDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private applicationService = inject(ApplicationService);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  application: JobApplication | null = null;
  loading = true;
  errorMessage = '';
  actionLoading = false;
  resumeLoading = false;

  UserRole = UserRole;

  get isHR(): boolean { return this.authService.getRole() === UserRole.CompanyHR; }
  get isAdmin(): boolean { return this.authService.getRole() === UserRole.Admin; }
  get isCandidate(): boolean { return this.authService.getRole() === UserRole.Candidate; }

  private readonly actionToStatus: Record<ApplicationAction, string> = {
    StartReview: 'UnderReview',
    Shortlist: 'Shortlisted',
    Accept: 'Accepted',
    Reject: 'Rejected',
    Withdraw: 'Withdrawn'
  };

  get hrActions(): { label: string; action: ApplicationAction; class: string }[] {
    if (!this.application) return [];
    const status = this.application.status;
    const actions: { label: string; action: ApplicationAction; class: string }[] = [];

    if (status === 'Submitted') {
      actions.push({ label: 'Start Review', action: 'StartReview', class: 'bg-yellow-500 hover:bg-yellow-600 text-white' });
    }
    if (status === 'UnderReview') {
      actions.push({ label: 'Shortlist', action: 'Shortlist', class: 'bg-purple-600 hover:bg-purple-700 text-white' });
      actions.push({ label: 'Reject', action: 'Reject', class: 'bg-red-600 hover:bg-red-700 text-white' });
    }
    if (status === 'Shortlisted') {
      actions.push({ label: 'Accept', action: 'Accept', class: 'bg-green-600 hover:bg-green-700 text-white' });
      actions.push({ label: 'Reject', action: 'Reject', class: 'bg-red-600 hover:bg-red-700 text-white' });
    }
    return actions;
  }

  get canWithdraw(): boolean {
    if (!this.isCandidate || !this.application) return false;
    return ['Submitted', 'UnderReview', 'Shortlisted'].includes(this.application.status);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/jobs']);
      return;
    }

    this.applicationService.getApplication(id).subscribe({
      next: (app) => {
        this.application = app;
        this.loading = false;
      },
      error: () => {
        // Fall back to localStorage (when deployed backend has HybridCache bug or endpoint missing)
        const apps: JobApplication[] = JSON.parse(localStorage.getItem('tb_my_applications') ?? '[]');
        const local = apps.find(a => a.id === id);
        if (local) {
          this.application = local;
        } else {
          this.errorMessage = 'Application not found or you do not have permission to view it.';
        }
        this.loading = false;
      }
    });
  }

  performAction(action: ApplicationAction): void {
    if (!this.application) return;
    this.actionLoading = true;

    const newStatus = this.actionToStatus[action];
    this.applicationService.updateStatus(this.application.id, newStatus).subscribe({
      next: () => {
        this.actionLoading = false;
        if (this.application) {
          this.application = { ...this.application, status: newStatus };
        }
        this.toast.success(`Status updated to ${newStatus}.`);
      },
      error: (err) => {
        this.actionLoading = false;
        this.toast.error(err?.error?.message ?? 'Failed to update status.');
      }
    });
  }

  viewResume(): void {
    if (!this.application) return;
    this.resumeLoading = true;
    this.applicationService.getResumePreview(this.application.id).subscribe({
      next: (preview: ResumePreview) => {
        this.resumeLoading = false;
        window.open(preview.url, '_blank');
      },
      error: () => {
        this.resumeLoading = false;
        this.toast.error('Resume not available. It may have been removed or not yet uploaded.');
      }
    });
  }

  formatDate(dateStr?: string): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'long', day: 'numeric', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
