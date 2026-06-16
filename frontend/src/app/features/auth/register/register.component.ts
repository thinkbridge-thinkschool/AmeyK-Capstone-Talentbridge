import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { UserRole } from '../../../core/models/user.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  form!: FormGroup;
  loading = false;
  errorMessage = '';
  successMessage = '';
  showPassword = false;

  roles = [
    { value: UserRole.Candidate, label: 'Candidate — Looking for jobs' },
    { value: UserRole.CompanyHR, label: 'Company HR — Posting jobs' }
  ];

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/)]],
      role: [UserRole.Candidate, Validators.required]
    });

    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/jobs']);
    }
  }

  get f() { return this.form.controls; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.register({
      email: this.form.value.email,
      password: this.form.value.password,
      role: Number(this.form.value.role)
    }).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = 'Account created successfully! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/auth/login']), 1800);
      },
      error: (err) => {
        this.loading = false;
        if (err?.error?.errors) {
          const msgs = Object.values(err.error.errors).flat() as string[];
          this.errorMessage = msgs.join(' ');
        } else {
          this.errorMessage = err?.error?.message ?? err?.error?.title ?? 'Registration failed. Please try again.';
        }
      }
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
}
