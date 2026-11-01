import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { ApiError } from '../../../core/api/api-error';
import { Dsp, TrackDetails, TrackStatus } from '../../../core/api/api.models';
import { NotificationService } from '../../../core/notifications/notification.service';
import { CopyToClipboardDirective } from '../../../shared/directives/copy-to-clipboard.directive';
import { IfRoleDirective } from '../../../shared/directives/if-role.directive';
import { StatusToneDirective } from '../../../shared/directives/status-tone.directive';
import { TracksApi } from '../data/tracks.api';

@Component({
  selector: 'app-track-detail',
  imports: [RouterLink, DatePipe, StatusToneDirective, CopyToClipboardDirective, IfRoleDirective],
  templateUrl: './track-detail.html',
  styleUrl: './track-detail.scss',
})
export class TrackDetail {
  private readonly api = inject(TracksApi);
  private readonly notifications = inject(NotificationService);

  readonly id = input.required<string>();

  protected readonly track = httpResource<TrackDetails>(() => `/api/tracks/${this.id()}`);
  protected readonly dsps = httpResource<Dsp[]>(() => '/api/dsps', { defaultValue: [] });

  protected readonly selectedDspIds = signal<ReadonlySet<string>>(new Set());
  protected readonly busy = signal(false);

  protected readonly availableDsps = computed(() => {
    const blocked = new Set(
      (this.track.hasValue() ? this.track.value().distributions : [])
        .filter((d) => d.status !== 'rejected')
        .map((d) => d.dspId),
    );
    return this.dsps.value().filter((dsp) => !blocked.has(dsp.id));
  });

  protected readonly liveCount = computed(() =>
    this.track.hasValue() ? this.track.value().distributions.filter((d) => d.status === 'live').length : 0,
  );

  protected toggleDsp(dspId: string, checked: boolean): void {
    this.selectedDspIds.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(dspId);
      } else {
        next.delete(dspId);
      }
      return next;
    });
  }

  protected changeStatus(status: TrackStatus): void {
    this.run(this.api.updateStatus(this.id(), status), `Status changed to "${status}".`);
  }

  protected distribute(): void {
    const dspIds = [...this.selectedDspIds()];
    if (dspIds.length === 0) {
      return;
    }
    this.run(this.api.distribute(this.id(), dspIds), `Submitted to ${dspIds.length} DSP(s).`, () =>
      this.selectedDspIds.set(new Set()),
    );
  }

  private run(request: ReturnType<TracksApi['updateStatus']>, successMessage: string, onSuccess?: () => void): void {
    this.busy.set(true);
    request.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: (updated) => {
        this.track.set(updated);
        onSuccess?.();
        this.notifications.success(successMessage);
      },
      error: (error: ApiError) => {
        if (error.status === 409) {
          this.track.reload();
        }
      },
    });
  }
}
