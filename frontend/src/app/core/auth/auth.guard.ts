import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { NotificationService } from '../notifications/notification.service';
import { AuthService } from './auth.service';

export const guestOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? inject(Router).createUrlTree(['/tracks']) : true;
};

export function roleGuard(role: string): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    if (!auth.hasRole(role)) {
      inject(NotificationService).error('You need the catalog-manager role to do that.');
      return router.createUrlTree(['/tracks']);
    }

    return true;
  };
}
