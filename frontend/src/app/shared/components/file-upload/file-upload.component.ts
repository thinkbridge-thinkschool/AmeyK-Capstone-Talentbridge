import { Component, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ResumeService } from '../../../core/services/resume.service';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.component.html'
})
export class FileUploadComponent {
  @Output() uploaded = new EventEmitter<string>();
  @Output() uploadError = new EventEmitter<string>();

  private resumeService = inject(ResumeService);

  dragOver = signal(false);
  uploading = signal(false);
  progress = signal(0);
  uploadedUrl = signal<string | null>(null);
  error = signal<string | null>(null);

  readonly ALLOWED = '.pdf, .doc, .docx';
  readonly MAX_SIZE_MB = 5;

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(): void { this.dragOver.set(false); }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this.processFile(file);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.processFile(file);
    input.value = '';
  }

  private processFile(file: File): void {
    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!['pdf', 'doc', 'docx'].includes(ext)) {
      this.error.set('Only PDF, DOC, and DOCX files are allowed.');
      return;
    }
    if (file.size > this.MAX_SIZE_MB * 1024 * 1024) {
      this.error.set(`File size must not exceed ${this.MAX_SIZE_MB}MB.`);
      return;
    }

    this.error.set(null);
    this.uploading.set(true);
    this.progress.set(0);

    this.resumeService.upload(file).subscribe({
      next: (evt) => {
        this.progress.set(evt.progress);
        if (evt.url) {
          this.uploadedUrl.set(evt.url);
          this.uploading.set(false);
          this.uploaded.emit(evt.url);
        }
      },
      error: (err) => {
        this.uploading.set(false);
        const msg = err?.error?.message ?? 'Upload failed. Please try again.';
        this.error.set(msg);
        this.uploadError.emit(msg);
      }
    });
  }

  reset(): void {
    this.uploadedUrl.set(null);
    this.error.set(null);
    this.progress.set(0);
  }
}
