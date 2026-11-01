import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
  details?: string[];
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  private readonly _toasts = signal<Toast[]>([]);

  readonly toasts = this._toasts.asReadonly();

  success(message: string): void {
    this.push('success', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  error(message: string, details: string[] = []): void {
    if (this._toasts().some((t) => t.kind === 'error' && t.message === message)) {
      return;
    }
    this.push('error', message, details, 8000);
  }

  dismiss(id: number): void {
    this._toasts.update((toasts) => toasts.filter((t) => t.id !== id));
  }

  private push(kind: ToastKind, message: string, details: string[] = [], timeoutMs = 4000): void {
    const toast: Toast = { id: this.nextId++, kind, message, details };
    this._toasts.update((toasts) => [...toasts, toast]);
    setTimeout(() => this.dismiss(toast.id), timeoutMs);
  }
}
