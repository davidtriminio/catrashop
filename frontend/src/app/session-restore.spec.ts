import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '@cs/api/api-base-url';
import { AuthTokens, UserProfile } from '@cs/api/models';
import { authInterceptor } from '@cs/auth/auth.interceptor';
import { AuthStore } from '@cs/auth/auth.store';

const tokens: AuthTokens = {
  accessToken: 'access-old', tokenType: 'Bearer', accessTokenExpiresAtUtc: '2000-01-01T00:00:00Z',
  refreshToken: 'refresh-old', refreshTokenExpiresAtUtc: '2000-01-02T00:00:00Z',
};
const profile = { id: 'u1', email: 'a@b.c', firstName: 'Ana', lastName: 'L', phone: null, roles: [], permissions: ['orders.read_all'] } as unknown as UserProfile;

/** The real store, the real interceptor and a fake backend: what happens when the page is reloaded with a stored session. */
function setup() {
  localStorage.clear();
  localStorage.setItem('cs_auth_tokens', JSON.stringify(tokens));
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      { provide: API_BASE_URL, useValue: '' },
    ],
  });
  return { store: TestBed.inject(AuthStore), ctl: TestBed.inject(HttpTestingController) };
}

describe('Restoring a stored session after a page reload', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('creating the store does not call the API by itself (the interceptor needs the finished store)', () => {
    const { store, ctl } = setup();
    ctl.expectNone('/api/v1/identity/me');
    expect(store.isAuthenticated()).toBe(true);
  });

  it('restore() loads the profile, so permissions are back', () => {
    const { store, ctl } = setup();

    store.restore();
    ctl.expectOne('/api/v1/identity/me').flush(profile);

    expect(store.user()?.firstName).toBe('Ana');
    expect(store.hasPermission('orders.read_all')).toBe(true);
  });

  it('an expired session ends: the profile call is refused, the refresh is refused, and the session is cleared', () => {
    const { store, ctl } = setup();

    store.restore();
    ctl.expectOne('/api/v1/identity/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    ctl.expectOne('/api/v1/identity/refresh').flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(store.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('cs_auth_tokens')).toBeNull();
  });

  it('a network failure keeps the session (only an explicit rejection ends it)', () => {
    const { store, ctl } = setup();

    store.restore();
    ctl.expectOne('/api/v1/identity/me').error(new ProgressEvent('error'), { status: 0 });

    expect(store.isAuthenticated()).toBe(true);
  });
});
