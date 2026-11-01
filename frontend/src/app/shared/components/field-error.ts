import { Component, computed, input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { map, merge, of, startWith, switchMap } from 'rxjs';


@Component({
  selector: 'app-field-error',
  template: `
    @if (message(); as text) {
      <small class="field-error" role="alert">{{ text }}</small>
    }
  `,
  styles: `.field-error { color: var(--danger); font-size: 12px; }`,
})
export class FieldError {
  readonly control = input.required<AbstractControl>();
  readonly label = input('This field');

  private readonly tick = toSignal(
    toObservable(this.control).pipe(
      switchMap((control) => merge(of(null), control.events).pipe(map(() => ({})))),
      startWith({}),
    ),
  );

  protected readonly message = computed(() => {
    this.tick();
    const control = this.control();
    if (!control.touched || !control.errors) {
      return null;
    }
    return describe(control.errors, this.label());
  });
}

function describe(errors: Record<string, unknown>, label: string): string {
  if (typeof errors['server'] === 'string') return errors['server'];
  if (errors['required']) return `${label} is required.`;
  if (errors['email']) return 'Enter a valid email address.';
  if (errors['maxlength']) {
    const { requiredLength } = errors['maxlength'] as { requiredLength: number };
    return `${label} must be at most ${requiredLength} characters.`;
  }
  if (errors['pattern']) return `${label} has an invalid format.`;
  return `${label} is invalid.`;
}
