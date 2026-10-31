import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, switchMap, tap } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { isSessionRejected } from './session-errors';
import { AuthTokens, LoginRequest, RegisterRequest, UserProfile } from '../api/models';

const STORAGE_KEY = 'cs_auth_tokens';

/** App-wide authentication state: tokens (persisted), the current profile, and derived permission checks. */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);

  private readonly tokens = signal<AuthTokens | null>(this.restore());
  readonly user = signal<UserProfile | null>(null);

  readonly isAuthenticated = computed(() => this.tokens() !== null);
  readonly permissions = computed(() => this.user()?.permissions ?? []);

  constructor() {
    if (this.tokens()) {
      // A transient failure (offline, 5xx) keeps the session; only an auth rejection ends it.
      this.loadProfile().subscribe({ error: (e: unknown) => { if (isSessionRejected(e)) this.clear(); } });
    }
  }

  accessToken(): string | null { return this.tokens()?.accessToken ?? null; }
  refreshTokenValue(): string | null { return this.tokens()?.refreshToken ?? null; }

  hasPermission(permission: string): boolean { return this.permissions().includes(permission); }

  login(request: LoginRequest): Observable<UserProfile> {
    return this.api.login(request).pipe(
      tap((tokens) => this.setSession(tokens)),
      switchMap(() => this.loadProfile()),
    );
  }

  register(request: RegisterRequest): Observable<UserProfile> {
    return this.api.register(request).pipe(
      tap((tokens) => this.setSession(tokens)),
      switchMap(() => this.loadProfile()),
    );
  }

  loadProfile(): Observable<UserProfile> {
    return this.api.me().pipe(tap((profile) => this.user.set(profile)));
  }

  setSession(tokens: AuthTokens): void {
    this.tokens.set(tokens);
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(tokens)); } catch { /* storage unavailable */ }
  }

  logout(): void {
    const refresh = this.refreshTokenValue();
    if (refresh) this.api.logout(refresh).subscribe({ error: () => {} });
    this.clear();
  }

  clear(): void {
    this.tokens.set(null);
    this.user.set(null);
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* storage unavailable */ }
  }

  private restore(): AuthTokens | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as AuthTokens) : null;
    } catch {
      return null;
    }
  }
}
