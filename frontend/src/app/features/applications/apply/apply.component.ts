import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { ApplicationService, ApplicationSummary } from '../../../core/services/application.service';
import { AuthService } from '../../../core/auth/auth.service';
import { JobService } from '../../../core/services/job.service';
import { Job } from '../../../core/models/job.model';
import { ToastService } from '../../../core/services/toast.service';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload.component';

@Component({
  selector: 'app-apply',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, FileUploadComponent],
  templateUrl: './apply.component.html'
})
export class ApplyComponent implements OnInit {
  private fb = inject(FormBuilder);
  private applicationService = inject(ApplicationService);
  private authService = inject(AuthService);
  private jobService = inject(JobService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);

  form!: FormGroup;
  job: Job | null = null;
  loading = false;
  jobLoading = true;
  jobId = '';
  fileUploaded = false;
  showResumeError = false;

  ngOnInit(): void {
    this.jobId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.jobId) {
      this.router.navigate(['/jobs']);
      return;
    }

    // Redirect if already applied
    const existing = this.applicationService.getExistingApplication(this.jobId);
    if (existing) {
      this.toast.info('You have already applied for this job.');
      this.router.navigate(['/applications', existing.id]);
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
      resumeUrl: ['', Validators.pattern(/^https?:\/\/.+/)]
    });
  }

  get f() { return this.form.controls; }

  get resumeProvided(): boolean {
    return this.fileUploaded || !!this.form.value.resumeUrl;
  }

  onResumeUploaded(url: string): void {
    this.fileUploaded = true;
    this.showResumeError = false;
    this.form.patchValue({ resumeUrl: url });
    this.form.get('resumeUrl')!.setErrors(null);
  }

  onSubmit(): void {
    this.showResumeError = !this.resumeProvided;
    if (this.form.invalid || !this.resumeProvided) {
      this.form.markAllAsTouched();
      return;
    }

    const candidateId = this.authService.getUserId();
    if (!candidateId) {
      this.toast.error('Unable to identify your account. Please log in again.');
      return;
    }

    this.loading = true;

    this.applicationService.apply({
      candidateId,
      jobId: this.jobId,
      coverLetter: this.form.value.coverLetter,
      resumeUrl: this.form.value.resumeUrl
    }).subscribe({
      next: (res) => {
        this.loading = false;
        const appId = res.applicationId;
        const apps: ApplicationSummary[] = JSON.parse(localStorage.getItem('tb_my_applications') ?? '[]');
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
        this.toast.success('Application submitted successfully!');
        setTimeout(() => this.router.navigate(['/applications', appId]), 1800);
      },
      error: (err) => {
        this.loading = false;
        this.toast.error(err?.error?.message ?? err?.error?.title ?? 'Failed to submit application. Please try again.');
      }
    });
  }
}
