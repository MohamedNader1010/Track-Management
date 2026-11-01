import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../core/api/api-error';
import { applyServerErrors } from '../../core/forms/server-errors';
import { NotificationService } from '../../core/notifications/notification.service';
import { FieldError } from '../../shared/components/field-error';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';
import { ArtistsApi } from './data/artists.api';

@Component({
  selector: 'app-artist-create',
  imports: [ReactiveFormsModule, RouterLink, FieldError, AutofocusDirective],
  template: `
    <a routerLink="/artists" class="back">‹ All artists</a>

    <div class="card form-card">
      <h1>New artist</h1>
      <p class="muted">Add an artist to the roster so their tracks can be distributed.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <label>
          <span>Name</span>
          <input formControlName="name" placeholder="Hana Mostafa" appAutofocus />
          <app-field-error [control]="form.controls.name" label="Name" />
        </label>

        <label>
          <span>Email</span>
          <input formControlName="email" type="email" placeholder="hana@example.com" />
          <app-field-error [control]="form.controls.email" label="Email" />
        </label>

        <label>
          <span>Country <small class="muted">(ISO code, e.g. EG, SA, AE)</small></span>
          <input formControlName="country" maxlength="2" placeholder="EG" class="short upper" />
          <app-field-error [control]="form.controls.country" label="Country" />
        </label>

        @if (formError(); as message) {
          <p class="form-error" role="alert">{{ message }}</p>
        }

        <div class="form-actions">
          <a routerLink="/artists" class="btn ghost">Cancel</a>
          <button type="submit" class="btn primary" [disabled]="submitting()">
            {{ submitting() ? 'Creating…' : 'Create artist' }}
          </button>
        </div>
      </form>
    </div>
  `,
})
export class ArtistCreate {
  private readonly api = inject(ArtistsApi);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    country: ['', [Validators.required, Validators.pattern(/^[A-Za-z]{2}$/)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);

    this.api
      .create(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (artist) => {
          this.notifications.success(`Artist "${artist.name}" created.`);
          void this.router.navigate(['/artists']);
        },
        error: (error: ApiError) => this.showError(error),
      });
  }

  private showError(error: ApiError): void {
    if (error.code === 'Artist.EmailAlreadyExists') {
      // Business conflict, but it's about one field — show it there.
      this.form.controls.email.setErrors({ server: error.message });
      return;
    }

    const unmatched = applyServerErrors(this.form, error);
    if (!error.isValidation || unmatched.length) {
      this.formError.set([error.message, ...unmatched].join(' '));
    }
  }
}
