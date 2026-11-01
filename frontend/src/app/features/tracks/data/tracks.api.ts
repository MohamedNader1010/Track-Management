import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CreateTrackRequest, TrackDetails, TrackStatus } from '../../../core/api/api.models';
import { silentErrors } from '../../../core/http/http-context.tokens';

/** Write operations. Reads use httpResource directly in the components (signal-based, cancel on change). */
@Injectable({ providedIn: 'root' })
export class TracksApi {
  private readonly http = inject(HttpClient);

  /** POST /api/tracks — errors are rendered by the form itself, so no global toast. */
  create(request: CreateTrackRequest): Observable<TrackDetails> {
    return this.http.post<TrackDetails>('/api/tracks', request, { context: silentErrors() });
  }

  updateStatus(trackId: string, status: TrackStatus): Observable<TrackDetails> {
    return this.http.patch<TrackDetails>(`/api/tracks/${trackId}/status`, { status });
  }

  distribute(trackId: string, dspIds: string[]): Observable<TrackDetails> {
    return this.http.post<TrackDetails>(`/api/tracks/${trackId}/distribute`, { dspIds });
  }
}
