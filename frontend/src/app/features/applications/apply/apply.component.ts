import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ApplicationService } from '../../../core/services/application.service';
import { AuthService } from '../../../core/auth/auth.service';
import { JobService } from '../../../core/services/job.service';
import { Job } from '../../../core/models/job.model';

@Component({
  selector: 'app-apply',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './apply.component.html'
})
export class ApplyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private applicationService = inject(ApplicationService);
  private authService = inject(AuthService);
  private jobService = inject(JobService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  form!: FormGroup;
  job: Job | null = null;
  loading = false;
  jobLoading = true;
  errorMessage = '';
  successMessage = '';
  jobId = '';

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.jobId) {
      this.router.navigate(['/jobs']);
      return;
    }

    // Load job details for context
    this.jobService.getJob(this.jobId).subscribe({
      next: (job) => {
        this.job = job;
        this.jobLoading = false;
      },
      error: () => {
        this.jobLoading = false;
      }
    });

    this.form = this.fb.group({
      coverLetter: ['', [Validators.required, Validators.minLength(50), Validators.maxLength(2000)]],
      resumeUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/)]]
    });
  }

  get f() { return this.form.controls; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const candidateId = this.authService.getUserId();
    if (!candidateId) {
      this.errorMessage = 'Unable to identify your account. Please log in again.';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.applicationService.apply({
      candidateId,
      jobId: this.jobId,
      coverLetter: this.form.value.coverLetter,
      resumeUrl: this.form.value.resumeUrl
    }).subscribe({
      next: (application) => {
        this.loading = false;
        // Store in localStorage so dashboard shows it even before backend is fully deployed
        const apps = JSON.parse(localStorage.getItem('tb_my_applications') ?? '[]');
        const appId = application.applicationId ?? application.id;
        apps.unshift({
          id: appId,
          candidateId,
          jobId: this.jobId,
          status: 'Submitted',
          coverLetter: this.form.value.coverLetter,
          resumeUrl: this.form.value.resumeUrl,
          submittedAtUtc: new Date().toISOString(),
          lastUpdatedAtUtc: new Date().toISOString(),
          jobTitle: this.job?.title ?? ''
        });
        localStorage.setItem('tb_my_applications', JSON.stringify(apps.slice(0, 50)));
        this.successMessage = 'Application submitted successfully!';
        setTimeout(() => this.router.navigate(['/applications', appId]), 1800);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message ?? err?.error?.title ?? 'Failed to submit application. Please try again.';
      }
    });
  }
}
