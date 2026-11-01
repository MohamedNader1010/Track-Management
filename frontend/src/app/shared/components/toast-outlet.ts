import { Component, inject } from '@angular/core';

import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-toast-outlet',
  template: `
    <div class="stack" aria-live="polite">
      @for (toast of notifications.toasts(); track toast.id) {
        <div class="toast" [attr.data-kind]="toast.kind" role="status">
          <div class="body">
            <strong>{{ toast.message }}</strong>
            @if (toast.details?.length) {
              <ul>
                @for (detail of toast.details; track $index) {
                  <li>{{ detail }}</li>
                }
              </ul>
            }
          </div>
          <button type="button" class="close" (click)="notifications.dismiss(toast.id)" aria-label="Dismiss">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .stack { position: fixed; right: 16px; bottom: 16px; display: grid; gap: 8px; z-index: 200; max-width: min(420px, calc(100vw - 32px)); }
    .toast {
      display: flex; gap: 12px; align-items: flex-start;
      padding: 12px 14px; border-radius: 10px; background: var(--surface);
      border: 1px solid var(--border); border-left: 4px solid var(--tone, var(--accent));
      box-shadow: 0 6px 24px rgb(0 0 0 / 0.12); font-size: 14px;
    }
    .toast[data-kind='success'] { --tone: var(--success); }
    .toast[data-kind='error'] { --tone: var(--danger); }
    .body { flex: 1; }
    ul { margin: 6px 0 0; padding-left: 18px; color: var(--muted); }
    .close { border: 0; background: none; font-size: 18px; line-height: 1; cursor: pointer; color: var(--muted); }
  `,
})
export class ToastOutlet {
  protected readonly notifications = inject(NotificationService);
}
