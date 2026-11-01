import { FormControl, FormGroup } from '@angular/forms';

import { ApiError } from '../api/api-error';
import { applyServerErrors } from './server-errors';

describe('applyServerErrors', () => {
  it('maps PascalCase API fields onto camelCase controls and returns the rest', () => {
    const form = new FormGroup({
      isrc: new FormControl('nope'),
      artistId: new FormControl(''),
    });
    const error = new ApiError(400, 'One or more validation errors occurred.', {
      Isrc: ['ISRC must follow the CC-XXX-YY-NNNNN format.'],
      ArtistId: ["'Artist Id' must not be empty."],
      Something: ['Unmapped problem.'],
    });

    const unmatched = applyServerErrors(form, error);

    expect(form.controls.isrc.errors?.['server']).toBe('ISRC must follow the CC-XXX-YY-NNNNN format.');
    expect(form.controls.isrc.touched).toBe(true);
    expect(form.controls.artistId.errors?.['server']).toContain('must not be empty');
    expect(unmatched).toEqual(['Unmapped problem.']);
  });
});
