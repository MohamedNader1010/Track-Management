import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, AutofocusDirective],
  template: `
    <div class="card login">
      <h1>Sign in</h1>
      <p class="muted">Catalog managers can create, distribute and publish tracks.</p>

      @if (expired()) {
        <p class="notice">Your session expired. Please sign in again.</p>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <label>
          <span>Username</span>
          <input formControlName="username" autocomplete="username" appAutofocus />
        </label>
        <label>
          <span>Password</span>
          <input formControlName="password" type="password" autocomplete="current-password" />
        </label>

        @if (error(); as message) {
          <p class="error" role="alert">{{ message }}</p>
        }

        <button type="submit" class="btn primary" [disabled]="form.invalid || submitting()">
          {{ submitting() ? 'Signing in…' : 'Sign in' }}
        </button>
      </form>

      <p class="muted demo">Demo: <code>manager / Takwene&#64;2026</code> (full access) or <code>viewer / Viewer&#64;2026</code> (read-only).</p>
    </div>
  `,
  styles: `
    .login { max-width: 400px; margin: 48px auto; }
    h1 { margin: 0 0 4px; }
    form { display: grid; gap: 14px; margin-top: 20px; }
    label { display: grid; gap: 6px; font-size: 14px; }
    input { width: 100%; }
    .error { color: var(--danger); margin: 0; font-size: 14px; }
    .notice { background: var(--surface-2); padding: 8px 12px; border-radius: 8px; font-size: 14px; }
    .demo { font-size: 13px; margin-top: 20px; }
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Query params from the error interceptor redirect. */
  readonly returnUrl = input<string>();
  readonly expired = input(false, { transform: (v: unknown) => v === true || v === 'true' });

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    username: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.maxLength(200)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }

    const { username, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.error.set(null);

    this.auth
      .login(username, password)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => void this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
        error: (err: ApiError) => this.error.set(err.message),
      });
  }
}

/** Only allow in-app paths — prevents an open redirect via ?returnUrl=https://evil.example. */
function safeReturnUrl(url: string | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') ? url : '/tracks';
}
