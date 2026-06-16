import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-hr-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './hr-dashboard.component.html'
})
export class HrDashboardComponent implements OnInit {
  private authService = inject(AuthService);

  currentUser: CurrentUser | null = null;

  workflowSteps = [
    { icon: '1', title: 'Post Job', desc: 'Create and publish a job listing' },
    { icon: '2', title: 'Receive Apps', desc: 'Candidates submit applications' },
    { icon: '3', title: 'Review', desc: 'Move apps through review pipeline' },
    { icon: '4', title: 'Accept', desc: 'Accept the best candidates' }
  ];

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }
}
