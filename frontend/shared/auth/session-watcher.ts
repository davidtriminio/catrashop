import { Injectable, effect, inject, untracked } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationCancel, NavigationEnd, NavigationError, Router } from '@angular/router';
import { filter, take } from 'rxjs';
import { AuthStore } from './auth.store';

/** True when the open page sits behind a guard, i.e. it needs a signed-in session. */
function needsSession(route: ActivatedRouteSnapshot): boolean {
  return !!route.routeConfig?.canActivate?.length || route.children.some(needsSession);
}

/**
 * Startup and session upkeep for both apps. At startup it reloads the profile of a stored session. Afterwards, if the
 * session is lost while a protected page is open (the refresh token expired, or another tab signed out), it sends the
 * person to the sign-in page, remembering where they were, instead of leaving an empty page behind.
 */
@Injectable({ providedIn: 'root' })
export class SessionWatcher {
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private signedIn = this.auth.isAuthenticated();

  constructor() {
    effect(() => {
      const now = this.auth.isAuthenticated();
      const lost = this.signedIn && !now;
      this.signedIn = now;
      if (lost) untracked(() => this.leaveProtectedPage());
    });
  }

  start(): void {
    this.auth.restore();
  }

  private leaveProtectedPage(settled = false): void {
    // The page is still opening (a reload restores the session while the first navigation runs): look again once it
    // has settled, so a sign-out the app is already navigating away from needs no second redirect.
    if (!settled && this.router.getCurrentNavigation()) {
      this.router.events
        .pipe(filter((e) => e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError), take(1))
        .subscribe(() => this.leaveProtectedPage(true));
      return;
    }
    if (!needsSession(this.router.routerState.snapshot.root)) return;
    void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
  }
}
