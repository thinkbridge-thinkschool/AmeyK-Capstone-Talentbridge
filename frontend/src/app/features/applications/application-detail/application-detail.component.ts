import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ApplicationService } from '../../../core/services/application.service';
import { AuthService } from '../../../core/auth/auth.service';
import { JobApplication, ApplicationAction } from '../../../core/models/application.model';
import { UserRole } from '../../../core/models/user.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-application-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBadgeComponent],
  templateUrl: './application-detail.component.html'
})
export class ApplicationDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private applicationService = inject(ApplicationService);
  private authService = inject(AuthService);

  application: JobApplication | null = null;
  loading = true;
  errorMessage = '';
  actionLoading = false;
  actionSuccess = '';
  actionError = '';

  UserRole = UserRole;

  get isHR(): boolean { return this.authService.getRole() === UserRole.CompanyHR; }
  get isAdmin(): boolean { return this.authService.getRole() === UserRole.Admin; }
  get isCandidate(): boolean { return this.authService.getRole() === UserRole.Candidate; }

  get hrActions(): { label: string; action: ApplicationAction; class: string }[] {
    if (!this.application) return [];
    const status = this.application.status;
    const actions: { label: string; action: ApplicationAction; class: string }[] = [];

    if (status === 'Submitted') {
      actions.push({ label: 'Start Review', action: 'StartReview', class: 'bg-yellow-500 hover:bg-yellow-600 text-white' });
    }
    if (status === 'Submitted' || status === 'UnderReview') {
      actions.push({ label: 'Shortlist', action: 'Shortlist', class: 'bg-purple-600 hover:bg-purple-700 text-white' });
    }
    if (status === 'Shortlisted' || status === 'UnderReview') {
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
        this.loading = false;
        this.errorMessage = 'Application not found or you do not have permission to view it.';
      }
    });
  }

  performAction(action: ApplicationAction): void {
    if (!this.application) return;
    this.actionLoading = true;
    this.actionSuccess = '';
    this.actionError = '';

    this.applicationService.updateStatus(this.application.id, action).subscribe({
      next: () => {
        this.actionLoading = false;
        const statusMap: Record<ApplicationAction, string> = {
          StartReview: 'UnderReview',
          Shortlist: 'Shortlisted',
          Accept: 'Accepted',
          Reject: 'Rejected',
          Withdraw: 'Withdrawn'
        };
        if (this.application) {
          this.application = { ...this.application, status: statusMap[action] };
        }
        this.actionSuccess = `Status updated to ${statusMap[action]}.`;
      },
      error: (err) => {
        this.actionLoading = false;
        this.actionError = err?.error?.message ?? 'Failed to update status.';
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
