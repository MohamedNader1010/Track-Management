import { Routes } from '@angular/router';

import { guestOnlyGuard } from './core/auth/auth.guard';

// Every feature is lazy-loaded as a standalone component — no NgModules anywhere.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tracks' },
  {
    path: 'tracks',
    title: 'Tracks · Takwene',
    loadComponent: () => import('./features/tracks/track-list/track-list').then((m) => m.TrackList),
  },
  {
    path: 'tracks/:id',
    title: 'Track details · Takwene',
    loadComponent: () => import('./features/tracks/track-detail/track-detail').then((m) => m.TrackDetail),
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
