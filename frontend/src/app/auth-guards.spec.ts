import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { authGuard } from '@ch/auth/auth.guard';
import { permissionGuard } from '@ch/auth/permission.guard';
import { AuthStore } from '@ch/auth/auth.store';

/** A fake Router that records createUrlTree calls without pulling in the full router. */
class FakeRouter {
  lastCommands: unknown[] | null = null;
  lastExtras: unknown = null;
  createUrlTree(commands: unknown[], extras?: unknown): UrlTree {
    this.lastCommands = commands;
    this.lastExtras = extras;
    return { __commands: commands, __extras: extras } as unknown as UrlTree;
  }
}

/** Minimal AuthStore stand-in exposing only what the guards read. */
class FakeAuthStore {
  private _authenticated = false;
  private _user: unknown = null;
  private _permissions: string[] = [];

  set(state: { authenticated: boolean; profileLoaded?: boolean; permissions?: string[] }) {
    this._authenticated = state.authenticated;
    this._user = state.profileLoaded ? {} : null;
    this._permissions = state.permissions ?? [];
  }
  isAuthenticated = () => this._authenticated;
  user = () => this._user;
  hasPermission = (p: string) => this._permissions.includes(p);
}

function setup() {
  const router = new FakeRouter();
  const store = new FakeAuthStore();
  TestBed.configureTestingModule({
    providers: [
      { provide: Router, useValue: router },
      { provide: AuthStore, useValue: store },
    ],
  });
  return { router, store };
}

const state = (url: string) => ({ url }) as { url: string };

describe('authGuard', () => {
  it('allows an authenticated user through', () => {
    const { store } = setup();
    store.set({ authenticated: true });
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, state('/account') as never));
    expect(result).toBe(true);
  });

  it('redirects an anonymous user to /login with a returnUrl', () => {
    const { router, store } = setup();
    store.set({ authenticated: false });
    TestBed.runInInjectionContext(() => authGuard({} as never, state('/account') as never));
    expect(router.lastCommands).toEqual(['/login']);
    expect(router.lastExtras).toEqual({ queryParams: { returnUrl: '/account' } });
  });
});

describe('permissionGuard', () => {
  it('redirects an anonymous user to /login', () => {
    const { router, store } = setup();
    store.set({ authenticated: false });
    TestBed.runInInjectionContext(() =>
      permissionGuard('catalog.products.manage')({} as never, state('/admin/catalog') as never),
    );
    expect(router.lastCommands).toEqual(['/login']);
  });

  it('sends an authenticated user lacking the permission to /forbidden', () => {
    const { router, store } = setup();
    store.set({ authenticated: true, profileLoaded: true, permissions: ['orders.manage'] });
    TestBed.runInInjectionContext(() =>
      permissionGuard('catalog.products.manage')({} as never, state('/admin/catalog') as never),
    );
    expect(router.lastCommands).toEqual(['/forbidden']);
  });

  it('grants access when the permission is present', () => {
    const { store } = setup();
    store.set({ authenticated: true, profileLoaded: true, permissions: ['catalog.products.manage'] });
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard('catalog.products.manage')({} as never, state('/admin/catalog') as never),
    );
    expect(result).toBe(true);
  });

  it('allows access through while the profile is still loading (backend stays the authority)', () => {
    const { store } = setup();
    store.set({ authenticated: true, profileLoaded: false });
    const result = TestBed.runInInjectionContext(() =>
      permissionGuard('catalog.products.manage')({} as never, state('/admin/catalog') as never),
    );
    expect(result).toBe(true);
  });
});
