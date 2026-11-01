import { httpResource } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../../core/api/api-error';
import { Artist, GENRES } from '../../../core/api/api.models';
import { applyServerErrors } from '../../../core/forms/server-errors';
import { NotificationService } from '../../../core/notifications/notification.service';
import { FieldError } from '../../../shared/components/field-error';
import { AutofocusDirective } from '../../../shared/directives/autofocus.directive';
import { TracksApi } from '../data/tracks.api';
import { generateIsrc } from './isrc-generator';

const ISRC_PATTERN = /^[A-Za-z]{2}-?[A-Za-z0-9]{3}-?\d{2}-?\d{5}$/;

@Component({
  selector: 'app-track-create',
  imports: [ReactiveFormsModule, RouterLink, FieldError, AutofocusDirective],
  template: `
    <a routerLink="/tracks" class="back">‹ All tracks</a>

    <div class="card form-card">
      <h1>New track</h1>
      <p class="muted">The track is created as a <strong>draft</strong>. You can distribute it to DSPs from its page.</p>

      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <label>
          <span>Title</span>
          <input formControlName="title" placeholder="Leil El Qahira" appAutofocus />
          <app-field-error [control]="form.controls.title" label="Title" />
        </label>

        <label>
          <span>Artist</span>
          <select formControlName="artistId">
            <option value="" disabled>Select an artist…</option>
            @for (artist of artists.value(); track artist.id) {
              <option [value]="artist.id">{{ artist.name }} ({{ artist.country }})</option>
            }
          </select>
          <app-field-error [control]="form.controls.artistId" label="Artist" />
        </label>

        <div class="row">
          <label>
            <span>ISRC</span>
            <div class="input-with-action">
              <input formControlName="isrc" placeholder="EG-TKW-26-00001" class="upper mono" />
              <button type="button" class="btn ghost" (click)="generateIsrc()" title="Generate a random valid ISRC">
                Generate
              </button>
            </div>
            <app-field-error [control]="form.controls.isrc" label="ISRC" />
          </label>

          <label>
            <span>Release date</span>
            <input formControlName="releaseDate" type="date" />
            <app-field-error [control]="form.controls.releaseDate" label="Release date" />
          </label>
        </div>

        <label>
          <span>Genre</span>
          <select formControlName="genre">
            <option value="" disabled>Select a genre…</option>
            @for (genre of genres; track genre) {
              <option [value]="genre">{{ genre }}</option>
            }
          </select>
          <app-field-error [control]="form.controls.genre" label="Genre" />
        </label>

        @if (formError(); as message) {
          <p class="form-error" role="alert">{{ message }}</p>
        }

        <div class="form-actions">
          <a routerLink="/tracks" class="btn ghost">Cancel</a>
          <button type="submit" class="btn primary" [disabled]="submitting()">
            {{ submitting() ? 'Creating…' : 'Create track' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: `
    .input-with-action { display: flex; gap: 8px; }
    .input-with-action input { flex: 1; min-width: 0; }
  `,
})
export class TrackCreate {
  private readonly api = inject(TracksApi);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  /** Optional ?artistId= from the Artists page "+ Track" link. */
  readonly artistId = input<string>();

  protected readonly genres = GENRES;
  protected readonly artists = httpResource<Artist[]>(() => '/api/artists', { defaultValue: [] });

  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    artistId: ['', Validators.required],
    isrc: ['', [Validators.required, Validators.pattern(ISRC_PATTERN)]],
    releaseDate: [today(), Validators.required],
    genre: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const preselected = this.artistId();
      if (preselected) {
        this.form.controls.artistId.setValue(preselected);
      }
    });
  }

  protected generateIsrc(): void {
    const { artistId, releaseDate } = this.form.getRawValue();
    const artist = this.artists.value().find((a) => a.id === artistId);

    const control = this.form.controls.isrc;
    control.setValue(generateIsrc({ countryCode: artist?.country, releaseDate }));
    control.markAsTouched();
  }

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
        next: (track) => {
          this.notifications.success(`Track "${track.title}" created as draft.`);
          void this.router.navigate(['/tracks', track.id]);
        },
        error: (error: ApiError) => this.showError(error),
      });
  }

  private showError(error: ApiError): void {
    if (error.code === 'Track.IsrcAlreadyExists') {
      this.form.controls.isrc.setErrors({ server: error.message });
      return;
    }
    if (error.code === 'Artist.NotFound') {
      this.form.controls.artistId.setErrors({ server: error.message });
      this.artists.reload();
      return;
    }

    const unmatched = applyServerErrors(this.form, error);
    if (!error.isValidation || unmatched.length) {
      this.formError.set([error.message, ...unmatched].join(' '));
    }
  }
}

function today(): string {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}
