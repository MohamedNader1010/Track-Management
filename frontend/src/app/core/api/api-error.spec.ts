import { HttpErrorResponse } from '@angular/common/http';

import { ApiError } from './api-error';

describe('ApiError.from', () => {
  it('maps validation problem details to field errors', () => {
    const error = ApiError.from(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'One or more validation errors occurred.',
          status: 400,
          errors: { Isrc: ['ISRC must follow the CC-XXX-YY-NNNNN format.'], Genre: ['Unknown genre.'] },
        },
      }),
    );

    expect(error.isValidation).toBe(true);
    expect(error.fieldMessages).toHaveLength(2);
    expect(error.message).toBe('One or more validation errors occurred.');
  });

  it('keeps the domain error code for conflicts', () => {
    const error = ApiError.from(
      new HttpErrorResponse({
        status: 409,
        error: { title: 'Cannot move a track from draft to distributed.', status: 409, errorCode: 'Track.InvalidStatusTransition' },
      }),
    );

    expect(error.status).toBe(409);
    expect(error.code).toBe('Track.InvalidStatusTransition');
  });

  it('explains when the API is unreachable', () => {
    const error = ApiError.from(new HttpErrorResponse({ status: 0 }));

    expect(error.message).toContain('Cannot reach the API');
  });

  it('falls back to a friendly message for empty 403 responses', () => {
    expect(ApiError.from(new HttpErrorResponse({ status: 403 })).message).toBe('You do not have permission to do that.');
  });
});
