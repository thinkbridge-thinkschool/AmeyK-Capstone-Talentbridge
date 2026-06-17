import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'jobs', pathMatch: 'full' },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      }
    ]
  },
  {
    path: 'jobs',
    loadComponent: () =>
      import('./features/jobs/job-list/job-list.component').then(m => m.JobListComponent)
  },
  {
    path: 'jobs/new',
    loadComponent: () =>
      import('./features/jobs/job-form/job-form.component').then(m => m.JobFormComponent),
    canActivate: [authGuard]
  },
  {
    path: 'jobs/:id',
    loadComponent: () =>
      import('./features/jobs/job-detail/job-detail.component').then(m => m.JobDetailComponent)
  },
  {
    path: 'jobs/:id/apply',
    loadComponent: () =>
      import('./features/applications/apply/apply.component').then(m => m.ApplyComponent),
    canActivate: [authGuard]
  },
  {
    path: 'applications/:id',
    loadComponent: () =>
      import('./features/applications/application-detail/application-detail.component').then(
        m => m.ApplicationDetailComponent
      ),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard/candidate',
    loadComponent: () =>
      import('./features/dashboard/candidate-dashboard/candidate-dashboard.component').then(
        m => m.CandidateDashboardComponent
      ),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard/hr',
    loadComponent: () =>
      import('./features/dashboard/hr-dashboard/hr-dashboard.component').then(
        m => m.HrDashboardComponent
      ),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: 'jobs' }
];
