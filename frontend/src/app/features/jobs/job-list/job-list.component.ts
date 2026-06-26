import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { JobService } from '../../../core/services/job.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Job } from '../../../core/models/job.model';
import { UserRole } from '../../../core/models/user.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-job-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, StatusBadgeComponent],
  templateUrl: './job-list.component.html'
})
export class JobListComponent implements OnInit {
  private jobService = inject(JobService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);

  jobs: Job[] = [];
  totalCount = 0;
  loading = false;
  errorMessage = '';

  searchForm!: FormGroup;
  currentPage = 1;
  pageSize = 9;

  UserRole = UserRole;
  skeletonItems = [1, 2, 3, 4, 5, 6];

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pages(): number[] {
    const total = this.totalPages;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const start = Math.max(1, this.currentPage - 2);
    const end = Math.min(total, this.currentPage + 2);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  get canPostJob(): boolean {
    const role = this.authService.getRole();
    return role === UserRole.CompanyHR || role === UserRole.Admin;
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  showFilters = false;

  ngOnInit(): void {
    const keyword = this.route.snapshot.queryParamMap.get('keyword') ?? '';
    const location = this.route.snapshot.queryParamMap.get('location') ?? '';

    this.searchForm = this.fb.group({
      keyword: [keyword],
      location: [location],
      salaryMin: [null],
      salaryMax: [null]
    });

    this.loadJobs();

    this.searchForm.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage = 1;
        this.loadJobs();
      });
  }

  get hasActiveFilters(): boolean {
    const { salaryMin, salaryMax } = this.searchForm.value;
    return salaryMin != null || salaryMax != null;
  }

  clearFilters(): void {
    this.searchForm.patchValue({ salaryMin: null, salaryMax: null });
  }

  loadJobs(): void {
    this.loading = true;
    this.errorMessage = '';
    const { keyword, location, salaryMin, salaryMax } = this.searchForm.value;

    this.jobService.searchJobs(keyword, location, this.currentPage, this.pageSize,
      salaryMin ?? undefined, salaryMax ?? undefined).subscribe({
      next: (result) => {
        this.jobs = result.items ?? [];
        this.totalCount = result.totalCount ?? 0;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = 'Failed to load jobs. Please try again.';
        console.error(err);
      }
    });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadJobs();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  clearSearch(): void {
    this.searchForm.reset({ keyword: '', location: '' });
  }

  formatSalary(min: number, max: number): string {
    const fmt = (n: number) => n >= 1000 ? `$${(n / 1000).toFixed(0)}k` : `$${n}`;
    return `${fmt(min)} – ${fmt(max)}`;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
