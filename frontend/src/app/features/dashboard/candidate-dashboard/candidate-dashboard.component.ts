import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { switchMap, startWith } from 'rxjs/operators';
import { AuthService, CurrentUser } from '../../../core/auth/auth.service';
import { ApplicationService, ApplicationSummary } from '../../../core/services/application.service';

@Component({
  selector: 'app-candidate-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './candidate-dashboard.component.html'
})
export class CandidateDashboardComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private applicationService = inject(ApplicationService);
  private pollSub?: Subscription;

  currentUser: CurrentUser | null = null;
  applications: ApplicationSummary[] = [];
  loadingApps = true;
  appsError = '';

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => { this.currentUser = user; });

    // Poll every 30 seconds, start immediately
    this.pollSub = interval(30_000).pipe(
      startWith(0),
      switchMap(() => this.applicationService.getMyApplications())
    ).subscribe({
      next: apps => {
        this.applications = apps;
        this.loadingApps = false;
        localStorage.setItem('tb_my_applications', JSON.stringify(apps));
      },
      error: () => {
        const local: ApplicationSummary[] = JSON.parse(localStorage.getItem('tb_my_applications') ?? '[]');
        this.applications = local;
        this.loadingApps = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.pollSub?.unsubscribe();
  }

  get stats() {
    return {
      total: this.applications.length,
      underReview: this.applications.filter(a => a.status === 'UnderReview').length,
      shortlisted: this.applications.filter(a => a.status === 'Shortlisted').length,
      accepted: this.applications.filter(a => a.status === 'Accepted').length,
    };
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Submitted:   'bg-gray-100 text-gray-600',
      UnderReview: 'bg-blue-100 text-blue-700',
      Shortlisted: 'bg-purple-100 text-purple-700',
      Accepted:    'bg-green-100 text-green-700',
      Rejected:    'bg-red-100 text-red-700',
      Withdrawn:   'bg-gray-100 text-gray-400'
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  matchPillClass(pct?: number): string {
    if (pct == null) return 'bg-gray-100 text-gray-400';
    if (pct >= 80) return 'bg-green-100 text-green-700';
    if (pct >= 50) return 'bg-orange-100 text-orange-700';
    return 'bg-red-100 text-red-600';
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
