import { HttpErrorResponse } from '@angular/common/http';

import { ProblemDetails } from './api.models';


export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors: Record<string, string[]> = {},
    readonly code?: string,
    readonly traceId?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  get isValidation(): boolean {
    return this.status === 400 && Object.keys(this.fieldErrors).length > 0;
  }

  get fieldMessages(): string[] {
    return Object.values(this.fieldErrors).flat();
  }

  static from(response: HttpErrorResponse): ApiError {
    if (response.status === 0) {
      return new ApiError(0, 'Cannot reach the API. Is the backend running on http://localhost:5080?');
    }

    const problem = isProblemDetails(response.error) ? response.error : undefined;
    const message =
      problem?.title ?? problem?.detail ?? defaultMessage(response.status) ?? response.message;

    return new ApiError(response.status, message, problem?.errors ?? {}, problem?.errorCode, problem?.traceId);
  }
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'status' in value);
}

function defaultMessage(status: number): string | undefined {
  switch (status) {
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to do that.';
    case 404:
      return 'Not found.';
    case 429:
      return 'Too many attempts. Please wait a minute and try again.';
    default:
      return status >= 500 ? 'The server ran into a problem. Please try again.' : undefined;
  }
}
