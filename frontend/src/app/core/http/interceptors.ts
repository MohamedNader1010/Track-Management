import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, tap, throwError } from 'rxjs';

import { ApiError } from '../api/api-error';
import { AuthService } from '../auth/auth.service';
import { LoadingService } from '../loading/loading.service';
import { LoggerService } from '../logging/logger.service';
import { NotificationService } from '../notifications/notification.service';
import { SKIP_ERROR_TOAST, SKIP_LOADER } from './http-context.tokens';

/*
 * Functional interceptors, registered in app.config.ts in this order:
 *   logging → loading → auth → error
 * Requests flow top-to-bottom, responses bottom-to-top, so the error interceptor sees raw HTTP errors first
 * and the logging interceptor measures the full round-trip including the others.
 */

/** Logs method, URL, status and duration of every call. */
export const loggingInterceptor: HttpInterceptorFn = (req, next) => {
  const logger = inject(LoggerService);
  const startedAt = performance.now();
  const elapsed = () => `${Math.round(performance.now() - startedAt)} ms`;

  return next(req).pipe(
    tap({
      next: (event) => {
        if (event instanceof HttpResponse) {
          logger.debug(`HTTP ${req.method} ${req.urlWithParams} → ${event.status} (${elapsed()})`);
        }
      },
      error: (error: unknown) => {
        const status = error instanceof HttpErrorResponse ? error.status : 'n/a';
        logger.warn(`HTTP ${req.method} ${req.urlWithParams} → ${status} (${elapsed()})`);
      },
    }),
  );
};

/** Drives the global loader. Opt out per request with the SKIP_LOADER context token. */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.context.get(SKIP_LOADER)) {
    return next(req);
  }

  const loading = inject(LoadingService);
  loading.begin();
  return next(req).pipe(finalize(() => loading.end()));
};

/** Attaches the bearer token — only to our own API, never to third-party URLs. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  const isApiCall = req.url.startsWith('/api/');

  if (!token || !isApiCall) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

/**
 * Turns every HTTP failure into an ApiError, shows a toast (unless SKIP_ERROR_TOAST) and handles
 * an expired/invalid session centrally.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);
  const auth = inject(AuthService);
  const logger = inject(LoggerService);

  return next(req).pipe(
    catchError((response: unknown) => {
      if (!(response instanceof HttpErrorResponse)) {
        return throwError(() => response);
      }

      const error = ApiError.from(response);

      if (error.status === 401 && auth.isAuthenticated()) {
        auth.expireAndRedirect();
      }

      if (error.status >= 500 || error.status === 0) {
        logger.error(`API failure on ${req.method} ${req.url}`, { status: error.status, traceId: error.traceId });
      }

      if (!req.context.get(SKIP_ERROR_TOAST)) {
        notifications.error(error.message, error.fieldMessages);
      }

      return throwError(() => error);
    }),
  );
};
