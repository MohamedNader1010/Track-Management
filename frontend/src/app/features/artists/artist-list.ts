import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Artist } from '../../core/api/api.models';
import { IfRoleDirective } from '../../shared/directives/if-role.directive';

/** GET /api/artists — everyone can browse; the "New artist" button only shows for catalog managers. */
@Component({
  selector: 'app-artist-list',
  imports: [RouterLink, DatePipe, IfRoleDirective],
  template: `
    <header class="page-head">
      <div>
        <h1>Artists</h1>
        <p class="muted">
          @if (artists.hasValue()) {
            {{ artists.value().length }} artist{{ artists.value().length === 1 ? '' : 's' }}
          } @else {
            Roster
          }
        </p>
      </div>
      <a *appIfRole="'catalog-manager'" routerLink="/artists/new" class="btn primary">+ New artist</a>
    </header>

    @if (artists.error()) {
      <div class="card empty">
        <p>Couldn't load artists.</p>
        <button type="button" class="btn" (click)="artists.reload()">Try again</button>
      </div>
    } @else if (artists.hasValue()) {
      @if (artists.value().length === 0) {
        <div class="card empty"><p class="muted">No artists yet.</p></div>
      } @else {
        <div class="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Country</th>
                <th>Added</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (artist of artists.value(); track artist.id) {
                <tr>
                  <td><strong>{{ artist.name }}</strong></td>
                  <td>{{ artist.email }}</td>
                  <td>{{ artist.country }}</td>
                  <td class="nowrap">{{ artist.createdAtUtc | date: 'mediumDate' }}</td>
                  <td class="row-actions">
                    <a [routerLink]="['/tracks']" [queryParams]="{ artistId: artist.id }">Tracks</a>
                    <a *appIfRole="'catalog-manager'" [routerLink]="['/tracks/new']" [queryParams]="{ artistId: artist.id }">+ Track</a>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    } @else {
      <div class="card" aria-busy="true"><p class="muted">Loading artists…</p></div>
    }
  `,
  styles: `
    .row-actions { display: flex; gap: 16px; justify-content: flex-end; white-space: nowrap; font-size: 14px; }
  `,
})
export class ArtistList {
  protected readonly artists = httpResource<Artist[]>(() => '/api/artists');
}
