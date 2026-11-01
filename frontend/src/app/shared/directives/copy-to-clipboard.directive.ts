import { Directive, inject, input } from '@angular/core';

import { LoggerService } from '../../core/logging/logger.service';
import { NotificationService } from '../../core/notifications/notification.service';

@Directive({
  selector: '[appCopyToClipboard]',
  host: {
    role: 'button',
    tabindex: '0',
    class: 'copyable',
    '[attr.aria-label]': '"Copy " + appCopyToClipboard()',
    '(click)': 'copy()',
    '(keydown.enter)': 'copy()',
  },
})
export class CopyToClipboardDirective {
  private readonly notifications = inject(NotificationService);
  private readonly logger = inject(LoggerService);

  readonly appCopyToClipboard = input.required<string>();

  protected async copy(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.appCopyToClipboard());
      this.notifications.info(`Copied ${this.appCopyToClipboard()}`);
    } catch (error) {
      this.logger.warn('Clipboard write failed', error);
    }
  }
}
