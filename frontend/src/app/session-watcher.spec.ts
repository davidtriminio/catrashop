import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthApi } from '@cs/api/auth.api';
import { AuthTokens } from '@cs/api/models';
import { authGuard } from '@cs/auth/auth.guard';
import { AuthStore } from '@cs/auth/auth.store';
import { SessionWatcher } from '@cs/auth/session-watcher';
import { of } from 'rxjs';

@Component({ template: '' })
class Page {}

const tokens = { accessToken: 'a', tokenType: 'Bearer', accessTokenExpiresAtUtc: '2999-01-01T00:00:00Z', refreshToken: 'r', refreshTokenExpiresAtUtc: '2999-01-01T00:00:00Z' } as AuthTokens;

function setup(storedSession: boolean) {
  localStorage.clear();
  if (storedSession) localStorage.setItem('cs_auth_tokens', JSON.stringify(tokens));
  TestBed.configureTestingModule({
    providers: [
      provideRouter([
        { path: 'login', component: Page },
        { path: 'private', canActivate: [authGuard], component: Page },
        { path: 'public', component: Page },
      ]),
      { provide: AuthApi, useValue: { me: () => of({ permissions: [] }), logout: () => of(void 0) } },
    ],
  });
  const watcher = TestBed.inject(SessionWatcher);
  return { router: TestBed.inject(Router), store: TestBed.inject(AuthStore), watcher };
}

describe('SessionWatcher', () => {
  it('sends the person to sign in, remembering the page, when the session is lost on a protected page', async () => {
    const { router, store } = setup(true);
    await router.navigateByUrl('/private');
    TestBed.tick();

    store.clear(); // e.g. the refresh token expired
    TestBed.tick();
    await new Promise((r) => setTimeout(r));

    expect(router.url).toBe('/login?returnUrl=%2Fprivate');
  });

  it('also redirects when the session is lost while the first page is still opening (a reload with an expired token)', async () => {
    const { router, store } = setup(true);
    TestBed.tick();

    const opening = router.navigateByUrl('/private'); // not awaited: the session ends mid-navigation
    store.clear();
    TestBed.tick();
    await opening;
    await new Promise((r) => setTimeout(r, 20));

    expect(router.url).toContain('/login');
  });

  it('leaves public pages alone when the session is lost', async () => {
    const { router, store } = setup(true);
    await router.navigateByUrl('/public');
    TestBed.tick();

    store.clear();
    TestBed.tick();
    await new Promise((r) => setTimeout(r));

    expect(router.url).toBe('/public');
  });

  it('start() reloads the profile of a stored session', () => {
    const { store, watcher } = setup(true);
    watcher.start();
    expect(store.user()).not.toBeNull();
  });
});
