import { FormGroup } from '@angular/forms';

import { ApiError } from '../api/api-error';

/**
 * Puts API validation errors (ValidationProblemDetails.errors, keyed by PascalCase property name like "Isrc")
 * onto the matching form controls ("isrc") as a `server` error, so they show next to the right input.
 *
 * @returns messages that didn't match any control — show them at form level.
 */
export function applyServerErrors(form: FormGroup, error: ApiError): string[] {
  const unmatched: string[] = [];
  const controlsByLowerName = new Map(
    Object.keys(form.controls).map((name) => [name.toLowerCase(), form.controls[name]] as const),
  );

  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const control = controlsByLowerName.get(field.toLowerCase());
    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }

  return unmatched;
}
