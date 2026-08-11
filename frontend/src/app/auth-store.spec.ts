import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthApi } from '@ch/api/auth.api';
import { AuthStore } from '@ch/auth/auth.store';
import { AuthTokens, UserProfile } from '@ch/api/models';

const tokens: AuthTokens = {
  accessToken: 'access-1',
  tokenType: 'Bearer',
  accessTokenExpiresAtUtc: '2999-01-01T00:00:00Z',
  refreshToken: 'refresh-1',
  refreshTokenExpiresAtUtc: '2999-01-01T00:00:00Z',
};

const profile: UserProfile = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'user@commercehub.test',
  firstName: 'Ada',
  lastName: 'Lovelace',
  emailConfirmed: true,
  roles: ['customer'],
  permissions: ['catalog.products.manage'],
};

function makeStore(api: Partial<AuthApi>) {
  TestBed.configureTestingModule({
    providers: [{ provide: AuthApi, useValue: api }],
  });
  return TestBed.inject(AuthStore);
}

describe('AuthStore', () => {
  beforeEach(() => localStorage.clear());

  it('starts unauthenticated with no persisted tokens', () => {
    const store = makeStore({ me: () => of(profile) });
    expect(store.isAuthenticated()).toBe(false);
    expect(store.accessToken()).toBeNull();
    expect(store.permissions()).toEqual([]);
  });

  it('login sets the session, loads the profile, and persists tokens', () => {
    const store = makeStore({ login: () => of(tokens), me: () => of(profile) });

    let emitted: UserProfile | undefined;
    store.login({ email: profile.email, password: 'pw' }).subscribe((p) => (emitted = p));

    expect(emitted).toEqual(profile);
    expect(store.isAuthenticated()).toBe(true);
    expect(store.accessToken()).toBe('access-1');
    expect(store.hasPermission('catalog.products.manage')).toBe(true);
    expect(store.hasPermission('orders.manage')).toBe(false);
    expect(localStorage.getItem('ch_auth_tokens')).toContain('access-1');
  });

  it('logout clears state and storage', () => {
    const store = makeStore({ login: () => of(tokens), me: () => of(profile), logout: () => of(void 0) });
    store.login({ email: profile.email, password: 'pw' }).subscribe();

    store.logout();

    expect(store.isAuthenticated()).toBe(false);
    expect(store.user()).toBeNull();
    expect(localStorage.getItem('ch_auth_tokens')).toBeNull();
  });

  it('restores a persisted session on construction', () => {
    localStorage.setItem('ch_auth_tokens', JSON.stringify(tokens));
    const store = makeStore({ me: () => of(profile) });

    expect(store.isAuthenticated()).toBe(true);
    expect(store.refreshTokenValue()).toBe('refresh-1');
  });
});
