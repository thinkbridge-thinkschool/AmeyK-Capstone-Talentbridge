import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService, CurrentUser } from '../../core/auth/auth.service';

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
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  currentUser: CurrentUser | null = null;
  form!: FormGroup;
  editMode = false;
  saved = false;

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
    this.saved = false;
  }

  cancel(): void {
    const stored = this.loadProfile();
    this.form.patchValue(stored);
    this.editMode = false;
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    localStorage.setItem(PROFILE_KEY, JSON.stringify(this.form.value));
    this.editMode = false;
    this.saved = true;
    setTimeout(() => { this.saved = false; }, 3000);
  }
}
