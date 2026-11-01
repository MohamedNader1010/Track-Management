import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Artist, CreateArtistRequest } from '../../../core/api/api.models';
import { silentErrors } from '../../../core/http/http-context.tokens';

@Injectable({ providedIn: 'root' })
export class ArtistsApi {
  private readonly http = inject(HttpClient);

  /** POST /api/artists — errors are rendered by the form itself, so no global toast. */
  create(request: CreateArtistRequest): Observable<Artist> {
    return this.http.post<Artist>('/api/artists', request, { context: silentErrors() });
  }
}
