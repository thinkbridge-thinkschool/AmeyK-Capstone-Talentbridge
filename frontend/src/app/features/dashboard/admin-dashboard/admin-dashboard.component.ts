import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminService, AdminUser } from '../../../core/services/admin.service';
import { JobService } from '../../../core/services/job.service';
import { CompanyService, Company } from '../../../core/services/company.service';
import { Job } from '../../../core/models/job.model';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);
  private jobService = inject(JobService);
  private companyService = inject(CompanyService);
  private toast = inject(ToastService);

  users: AdminUser[] = [];
  jobs: Job[] = [];
  companies: Company[] = [];
  loadingUsers = true;
  loadingJobs = true;
  loadingCompanies = true;
  usersError = '';
  jobsError = '';
  activeTab: 'users' | 'jobs' | 'companies' = 'users';

  ngOnInit(): void {
    this.adminService.getAllUsers().subscribe({
      next: users => { this.users = users; this.loadingUsers = false; },
      error: () => {
        this.usersError = 'Failed to load users.';
        this.loadingUsers = false;
        this.toast.error('Failed to load users.');
      }
    });

    this.adminService.getAllJobs().subscribe({
      next: jobs => { this.jobs = jobs; this.loadingJobs = false; },
      error: () => {
        this.jobsError = 'Failed to load jobs.';
        this.loadingJobs = false;
        this.toast.error('Failed to load jobs.');
      }
    });

    this.companyService.getAllCompanies().subscribe({
      next: companies => { this.companies = companies; this.loadingCompanies = false; },
      error: () => { this.loadingCompanies = false; }
    });
  }

  deactivateUser(id: string): void {
    this.adminService.deactivateUser(id).subscribe({
      next: () => {
        const user = this.users.find(u => u.id === id);
        if (user) user.isActive = false;
        this.toast.success('User deactivated successfully.');
      },
      error: () => this.toast.error('Failed to deactivate user.')
    });
  }

  approveCompany(id: string): void {
    this.companyService.approveCompany(id).subscribe({
      next: () => {
        const company = this.companies.find(c => c.id === id);
        if (company) company.isApproved = true;
        this.toast.success('Company approved!');
      },
      error: () => this.toast.error('Failed to approve company.')
    });
  }

  get pendingCompanies(): Company[] {
    return this.companies.filter(c => !c.isApproved);
  }

  roleBadge(role: string): string {
    const map: Record<string, string> = {
      Admin: 'bg-red-100 text-red-700',
      CompanyHR: 'bg-purple-100 text-purple-700',
      Candidate: 'bg-blue-100 text-blue-700'
    };
    return map[role] ?? 'bg-gray-100 text-gray-600';
  }

  statusBadge(status: string): string {
    const map: Record<string, string> = {
      Active: 'bg-green-100 text-green-700',
      Draft: 'bg-gray-100 text-gray-600',
      Closed: 'bg-red-100 text-red-700',
      Expired: 'bg-orange-100 text-orange-600'
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
