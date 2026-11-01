import { ErrorHandler, Injectable, inject } from '@angular/core';

import { ApiError } from '../api/api-error';
import { LoggerService } from '../logging/logger.service';
import { NotificationService } from '../notifications/notification.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly logger = inject(LoggerService);
  private readonly notifications = inject(NotificationService);

  handleError(error: unknown): void {
    const unwrapped = unwrap(error);

    if (unwrapped instanceof ApiError) {
      this.logger.debug('Unhandled API error (already reported)', unwrapped);
      return;
    }

    this.logger.error('Unexpected client error', unwrapped);
    this.notifications.error('Something went wrong in the app. Please refresh the page.');
  }
}

function unwrap(error: unknown): unknown {
  if (typeof error === 'object' && error !== null && 'rejection' in error) {
    return (error as { rejection: unknown }).rejection;
  }
  return error;
}
