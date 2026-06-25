import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Toast, ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html'
})
export class ToastComponent {
  readonly toastService = inject(ToastService);

  remove(id: number): void { this.toastService.remove(id); }

  trackById(_: number, t: Toast): number { return t.id; }

  bgClass(type: string): string {
    const map: Record<string, string> = {
      success: 'bg-emerald-600',
      error: 'bg-red-600',
      warning: 'bg-amber-500',
      info: 'bg-blue-600'
    };
    return map[type] ?? 'bg-gray-700';
  }

  iconPath(type: string): string {
    if (type === 'success') return 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z';
    if (type === 'error')   return 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z';
    if (type === 'warning') return 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z';
    return 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z';
  }
}
