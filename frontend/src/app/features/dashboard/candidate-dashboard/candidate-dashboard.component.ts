import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../../../core/auth/auth.service';
import { ApplicationService, ApplicationSummary } from '../../../core/services/application.service';

@Component({
  selector: 'app-candidate-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './candidate-dashboard.component.html'
})
export class CandidateDashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private applicationService = inject(ApplicationService);

  currentUser: CurrentUser | null = null;
  applications: ApplicationSummary[] = [];
  loadingApps = true;
  appsError = '';

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    this.applicationService.getMyApplications().subscribe({
      next: apps => {
        this.applications = apps;
        this.loadingApps = false;
      },
      error: () => {
        const local: ApplicationSummary[] = JSON.parse(localStorage.getItem('tb_my_applications') ?? '[]');
        this.applications = local;
        this.loadingApps = false;
      }
    });
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Submitted: 'bg-blue-100 text-blue-700',
      UnderReview: 'bg-yellow-100 text-yellow-700',
      Shortlisted: 'bg-purple-100 text-purple-700',
      Accepted: 'bg-green-100 text-green-700',
      Rejected: 'bg-red-100 text-red-700',
      Withdrawn: 'bg-gray-100 text-gray-500'
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
