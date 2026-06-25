import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService, CurrentUser } from '../../core/auth/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { FileUploadComponent } from '../../shared/components/file-upload/file-upload.component';
import { environment } from '../../../environments/environment';

interface CandidateProfile {
  fullName: string;
  phone: string;
  location: string;
  title: string;
  bio: string;
  skills: string;
  resumeUrl: string;
  linkedinUrl: string;
  githubUrl: string;
}

const PROFILE_KEY = 'tb_candidate_profile';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, FileUploadComponent],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);
  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  currentUser: CurrentUser | null = null;
  form!: FormGroup;
  editMode = false;
  saving = false;

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(u => { this.currentUser = u; });

    const stored = this.loadProfile();
    this.form = this.fb.group({
      fullName: [stored.fullName, Validators.required],
      phone: [stored.phone],
      location: [stored.location],
      title: [stored.title],
      bio: [stored.bio],
      skills: [stored.skills],
      resumeUrl: [stored.resumeUrl],
      linkedinUrl: [stored.linkedinUrl],
      githubUrl: [stored.githubUrl]
    });

    // Load profile from backend and merge with localStorage
    this.http.get<any>(`${this.apiUrl}/api/identity/me`).subscribe({
      next: (user) => {
        const merged = {
          fullName: user.fullName || stored.fullName,
          phone: user.phone || stored.phone,
          location: stored.location,
          title: user.title || stored.title,
          bio: user.bio || stored.bio,
          skills: user.skills || stored.skills,
          resumeUrl: user.resumeUrl || stored.resumeUrl,
          linkedinUrl: user.linkedInUrl || stored.linkedinUrl,
          githubUrl: user.gitHubUrl || stored.githubUrl
        };
        this.form.patchValue(merged);
        localStorage.setItem(PROFILE_KEY, JSON.stringify(merged));
      }
    });
  }

  private loadProfile(): CandidateProfile {
    try {
      const raw = localStorage.getItem(PROFILE_KEY);
      if (raw) return JSON.parse(raw) as CandidateProfile;
    } catch { /* ignore */ }
    return { fullName: '', phone: '', location: '', title: '', bio: '', skills: '', resumeUrl: '', linkedinUrl: '', githubUrl: '' };
  }

  get profile(): CandidateProfile {
    return this.loadProfile();
  }

  get skillsList(): string[] {
    return this.profile.skills
      ? this.profile.skills.split(',').map(s => s.trim()).filter(Boolean)
      : [];
  }

  get initials(): string {
    const name = this.profile.fullName || this.currentUser?.email || '?';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  startEdit(): void {
    this.editMode = true;
  }

  cancel(): void {
    const stored = this.loadProfile();
    this.form.patchValue(stored);
    this.editMode = false;
  }

  onResumeUploaded(url: string): void {
    this.form.patchValue({ resumeUrl: url });
    this.toast.success('Resume uploaded!');
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving = true;
    const v = this.form.value;

    this.http.patch(`${this.apiUrl}/api/identity/profile`, {
      fullName: v.fullName,
      phone: v.phone || null,
      title: v.title || null,
      bio: v.bio || null,
      skills: v.skills || null,
      resumeUrl: v.resumeUrl || null,
      linkedInUrl: v.linkedinUrl || null,
      gitHubUrl: v.githubUrl || null
    }).subscribe({
      next: () => {
        localStorage.setItem(PROFILE_KEY, JSON.stringify(v));
        this.editMode = false;
        this.saving = false;
        this.toast.success('Profile saved!');
      },
      error: () => {
        // Save locally even if API fails
        localStorage.setItem(PROFILE_KEY, JSON.stringify(v));
        this.editMode = false;
        this.saving = false;
        this.toast.warning('Saved locally. Changes will sync when online.');
      }
    });
  }
}
