import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';

/**
 * Guards a route by permission. Unauthenticated users go to /login (with returnUrl). Authenticated users whose
 * profile is loaded but who lack the permission are sent to /forbidden. If the profile is still loading (e.g. a
 * hard refresh restoring a token), access is allowed through — the backend remains the real authority and returns
 * 403, and the route re-guards on the next navigation.
 */
export function permissionGuard(permission: string): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthStore);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }
    const profileLoaded = auth.user() !== null;
    if (profileLoaded && !auth.hasPermission(permission)) {
      return router.createUrlTree(['/forbidden']);
    }
    return true;
  };
}
