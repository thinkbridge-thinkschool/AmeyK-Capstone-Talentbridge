import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CompanyService, Company } from '../../../core/services/company.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-company-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './company-list.component.html'
})
export class CompanyListComponent implements OnInit {
  private companyService = inject(CompanyService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  companies: Company[] = [];
  loading = true;
  showForm = false;
  submitting = false;

  form!: FormGroup;

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.maxLength(2000)]],
      website: ['', Validators.maxLength(500)]
    });
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.loading = true;
    this.companyService.getMyCompanies().subscribe({
      next: companies => { this.companies = companies; this.loading = false; },
      error: () => { this.loading = false; this.toast.error('Failed to load companies.'); }
    });
  }

  get f() { return this.form.controls; }

  openForm(): void { this.showForm = true; this.form.reset(); }
  closeForm(): void { this.showForm = false; }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.companyService.createCompany(this.form.value).subscribe({
      next: () => {
        this.submitting = false;
        this.showForm = false;
        this.toast.success('Company created! Pending admin approval.');
        this.loadCompanies();
      },
      error: (err) => {
        this.submitting = false;
        this.toast.error(err?.error?.message ?? 'Failed to create company.');
      }
    });
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
