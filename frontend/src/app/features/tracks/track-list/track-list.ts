import { httpResource } from '@angular/common/http';
import { Component, computed, inject, input, linkedSignal, numberAttribute } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';

import { Artist, PagedResponse, TRACK_STATUSES, TrackSummary } from '../../../core/api/api.models';
import { CopyToClipboardDirective } from '../../../shared/directives/copy-to-clipboard.directive';
import { StatusToneDirective } from '../../../shared/directives/status-tone.directive';

const PAGE_SIZE = 10;

/**
 * Track list. Filters live in the URL (?status=&artistId=&page=) so a filtered view can be bookmarked or shared;
 * they arrive as signal inputs thanks to withComponentInputBinding().
 */
@Component({
  selector: 'app-track-list',
  imports: [RouterLink, DatePipe, StatusToneDirective, CopyToClipboardDirective],
  templateUrl: './track-list.html',
  styleUrl: './track-list.scss',
})
export class TrackList {
  private readonly router = inject(Router);

  // Query params → signal inputs.
  readonly status = input<string>();
  readonly artistId = input<string>();
  readonly page = input(1, { transform: (v: unknown) => numberAttribute(v, 1) });

  protected readonly statuses = TRACK_STATUSES;

  protected readonly artists = httpResource<Artist[]>(() => '/api/artists', { defaultValue: [] });

  protected readonly tracks = httpResource<PagedResponse<TrackSummary>>(() => ({
    url: '/api/tracks',
    params: {
      page: this.page(),
      pageSize: PAGE_SIZE,
      ...(this.status() ? { status: this.status()! } : {}),
      ...(this.artistId() ? { artistId: this.artistId()! } : {}),
    },
  }));

  /** Last loaded page — kept while the next one loads, so the table dims instead of flashing a skeleton. */
  protected readonly loadedPage = linkedSignal<PagedResponse<TrackSummary> | undefined, PagedResponse<TrackSummary> | undefined>({
    source: () => (this.tracks.hasValue() ? this.tracks.value() : undefined),
    computation: (current, previous) => current ?? previous?.value,
  });

  protected readonly hasFilters = computed(() => !!this.status() || !!this.artistId());

  protected applyFilter(changes: { status?: string | null; artistId?: string | null }): void {
    void this.router.navigate([], {
      queryParams: { ...normalise(changes), page: null },
      queryParamsHandling: 'merge',
    });
  }

  protected goToPage(page: number): void {
    void this.router.navigate([], { queryParams: { page }, queryParamsHandling: 'merge' });
  }

  protected clearFilters(): void {
    void this.router.navigate([], { queryParams: {} });
  }
}

// Empty select values become `null`, which removes the query param.
function normalise(changes: Record<string, string | null | undefined>): Record<string, string | null> {
  return Object.fromEntries(Object.entries(changes).map(([key, value]) => [key, value ? value : null]));
}
