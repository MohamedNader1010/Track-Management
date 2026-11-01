import { Routes } from '@angular/router';

import { guestOnlyGuard, roleGuard } from './core/auth/auth.guard';

const CATALOG_MANAGER = 'catalog-manager';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tracks' },
  {
    path: 'tracks',
    title: 'Tracks · Takwene',
    loadComponent: () => import('./features/tracks/track-list/track-list').then((m) => m.TrackList),
  },
  {
    path: 'tracks/new',
    title: 'New track · Takwene',
    canActivate: [roleGuard(CATALOG_MANAGER)],
    loadComponent: () => import('./features/tracks/track-create/track-create').then((m) => m.TrackCreate),
  },
  {
    path: 'tracks/:id',
    title: 'Track details · Takwene',
    loadComponent: () => import('./features/tracks/track-detail/track-detail').then((m) => m.TrackDetail),
  },
  {
    path: 'artists',
    title: 'Artists · Takwene',
    loadComponent: () => import('./features/artists/artist-list').then((m) => m.ArtistList),
  },
  {
    path: 'artists/new',
    title: 'New artist · Takwene',
    canActivate: [roleGuard(CATALOG_MANAGER)],
    loadComponent: () => import('./features/artists/artist-create').then((m) => m.ArtistCreate),
  },
  {
    path: 'login',
    title: 'Sign in · Takwene',
    canActivate: [guestOnlyGuard],
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: '**',
    title: 'Not found · Takwene',
    loadComponent: () => import('./features/not-found').then((m) => m.NotFound),
  },
];
