import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { JobService } from '../../../core/services/job.service';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-job-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './job-form.component.html'
})
export class JobFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private jobService = inject(JobService);
  private authService = inject(AuthService);
  private router = inject(Router);

  form!: FormGroup;
  loading = false;
  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    // CompanyId would normally come from the HR's company profile.
    // We pre-fill from token where available; user can override.
    const userId = this.authService.getUserId() ?? '';

    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
      description: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(5000)]],
      location: ['', [Validators.required]],
      salaryMin: [null, [Validators.required, Validators.min(0)]],
      salaryMax: [null, [Validators.required, Validators.min(0)]],
      companyId: [userId, [Validators.required]],
      postedByHRId: [userId, [Validators.required]]
    }, { validators: this.salaryRangeValidator });
  }

  salaryRangeValidator(group: FormGroup) {
    const min = group.get('salaryMin')?.value;
    const max = group.get('salaryMax')?.value;
    if (min !== null && max !== null && min > max) {
      return { salaryRange: true };
    }
    return null;
  }

  get f() { return this.form.controls; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.jobService.postJob({
      title: this.form.value.title,
      description: this.form.value.description,
      location: this.form.value.location,
      salaryMin: Number(this.form.value.salaryMin),
      salaryMax: Number(this.form.value.salaryMax),
      companyId: this.form.value.companyId,
      postedByHRId: this.form.value.postedByHRId
    }).subscribe({
      next: (job) => {
        this.loading = false;
        this.successMessage = 'Job posted successfully! Redirecting...';
        setTimeout(() => this.router.navigate(['/jobs', job.id]), 1500);
      },
      error: (err) => {
        this.loading = false;
        if (err?.error?.errors) {
          const msgs = Object.values(err.error.errors).flat() as string[];
          this.errorMessage = msgs.join(' ');
        } else {
          this.errorMessage = err?.error?.message ?? err?.error?.title ?? 'Failed to post job. Please try again.';
        }
      }
    });
  }
}
