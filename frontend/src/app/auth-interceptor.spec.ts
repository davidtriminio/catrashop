import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthStore } from '@ch/auth/auth.store';
import { authInterceptor } from '@ch/auth/auth.interceptor';
import { AuthApi } from '@ch/api/auth.api';
import { AuthTokens } from '@ch/api/models';
import { Subject, of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

const stale: AuthTokens = {
  accessToken: 'old', tokenType: 'Bearer', accessTokenExpiresAtUtc: '2000-01-01T00:00:00Z',
  refreshToken: 'r0', refreshTokenExpiresAtUtc: '2999-01-01T00:00:00Z',
};
const fresh: AuthTokens = { ...stale, accessToken: 'new', refreshToken: 'r1' };

function setup(refresh: () => unknown) {
  localStorage.clear();
  localStorage.setItem('ch_auth_tokens', JSON.stringify(stale));
  const api = { refresh: counted(refresh), me: () => of({ permissions: [] }), logout: () => of(void 0) };
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      { provide: AuthApi, useValue: api },
    ],
  });
  return { api, http: TestBed.inject(HttpClient), ctl: TestBed.inject(HttpTestingController), store: TestBed.inject(AuthStore) };
}

function counted(impl: () => unknown) {
  const fn = (() => { fn.calls++; return impl(); }) as unknown as { (): unknown; calls: number };
  fn.calls = 0;
  return fn;
}

const unauthorized = { status: 401, statusText: 'Unauthorized' };

describe('authInterceptor refresh', () => {
  it('spends the refresh token once for parallel 401s and retries all requests', () => {
    const gate = new Subject<AuthTokens>();
    const { api, http, ctl, store } = setup(() => gate);
    const results: string[] = [];

    http.get('/api/v1/a').subscribe((r) => results.push('a:' + r));
    http.get('/api/v1/b').subscribe((r) => results.push('b:' + r));
    ctl.expectOne('/api/v1/a').flush(null, unauthorized);
    ctl.expectOne('/api/v1/b').flush(null, unauthorized);

    expect(api.refresh.calls).toBe(1);
    gate.next(fresh);
    gate.complete();

    ctl.expectOne((r) => r.url === '/api/v1/a' && r.headers.get('Authorization') === 'Bearer new').flush('ok');
    ctl.expectOne((r) => r.url === '/api/v1/b' && r.headers.get('Authorization') === 'Bearer new').flush('ok');
    expect(results.sort()).toEqual(['a:ok', 'b:ok']);
    expect(store.isAuthenticated()).toBe(true);
  });

  it('keeps the session when the refresh fails transiently (5xx, 429, offline)', () => {
    for (const status of [0, 429, 503]) {
      TestBed.resetTestingModule();
      const { http, ctl, store } = setup(() => throwError(() => new HttpErrorResponse({ status })));
      let failed = false;
      http.get('/api/v1/a').subscribe({ error: () => (failed = true) });
      ctl.expectOne('/api/v1/a').flush(null, unauthorized);
      expect(failed).toBe(true);
      expect(store.isAuthenticated()).toBe(true);
    }
  });

  it('clears the session when the server rejects the refresh token', () => {
    const { http, ctl, store } = setup(() => throwError(() => new HttpErrorResponse({ status: 401 })));
    http.get('/api/v1/a').subscribe({ error: () => {} });
    ctl.expectOne('/api/v1/a').flush(null, unauthorized);
    expect(store.isAuthenticated()).toBe(false);
  });

  it('restores the persisted session on reload and logout clears it', () => {
    const { store } = setup(() => of(fresh));
    expect(store.isAuthenticated()).toBe(true);
    expect(store.accessToken()).toBe('old');
    store.logout();
    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('ch_auth_tokens')).toBeNull();
  });
});
