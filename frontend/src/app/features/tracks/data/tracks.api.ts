import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { TrackDetails, TrackStatus } from '../../../core/api/api.models';

/** Write operations. Reads use httpResource directly in the components (signal-based, cancel on change). */
@Injectable({ providedIn: 'root' })
export class TracksApi {
  private readonly http = inject(HttpClient);

  updateStatus(trackId: string, status: TrackStatus): Observable<TrackDetails> {
    return this.http.patch<TrackDetails>(`/api/tracks/${trackId}/status`, { status });
  }

  distribute(trackId: string, dspIds: string[]): Observable<TrackDetails> {
    return this.http.post<TrackDetails>(`/api/tracks/${trackId}/distribute`, { dspIds });
  }
}
