import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../../../core/auth/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { UserRole } from '../../../core/models/user.model';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html'
})
export class NavbarComponent implements OnInit {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  currentUser: CurrentUser | null = null;
  mobileMenuOpen = false;
  unreadCount = 0;
  UserRole = UserRole;

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) this.loadUnreadCount();
    });
  }

  private loadUnreadCount(): void {
    this.notificationService.getNotifications().subscribe({
      next: ns => this.unreadCount = ns.filter(n => !n.isRead).length,
      error: () => {}
    });
  }

  get roleBadgeText(): string {
    if (!this.currentUser) return '';
    switch (this.currentUser.role) {
      case UserRole.Candidate: return 'Candidate';
      case UserRole.CompanyHR: return 'HR';
      case UserRole.Admin:     return 'Admin';
      default:                 return '';
    }
  }

  get roleBadgeClass(): string {
    if (!this.currentUser) return '';
    switch (this.currentUser.role) {
      case UserRole.Candidate: return 'bg-blue-500';
      case UserRole.CompanyHR: return 'bg-purple-500';
      case UserRole.Admin:     return 'bg-red-500';
      default:                 return 'bg-gray-500';
    }
  }

  get dashboardRoute(): string {
    if (!this.currentUser) return '/';
    switch (this.currentUser.role) {
      case UserRole.CompanyHR: return '/dashboard/hr';
      case UserRole.Admin:     return '/dashboard/admin';
      default:                 return '/dashboard/candidate';
    }
  }

  logout(): void {
    this.authService.logout();
  }

  toggleMobile(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }
}
