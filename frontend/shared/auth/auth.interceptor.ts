import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { AuthTokens } from '../api/models';
import { AuthStore } from './auth.store';
import { isSessionRejected } from './session-errors';

const AUTH_FREE = ['/identity/login', '/identity/register', '/identity/refresh'];
const RETRY_HEADER = 'X-Auth-Retried';

function isAuthFree(url: string): boolean {
  return AUTH_FREE.some((path) => url.includes(path));
}

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

/** The refresh currently in flight, shared so parallel 401s spend the single-use refresh token only once. */
let inflight: Observable<AuthTokens> | null = null;

/**
 * Attaches the bearer token and, on a 401, transparently refreshes once (rotating the token) and retries the
 * original request. Concurrent 401s share one refresh call (the refresh token is single-use). The session is
 * cleared only when the server rejects the refresh token, never for transient failures. Auth endpoints are
 * exempt, and a retry marker prevents an infinite refresh loop.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(AuthStore);
  const api = inject(AuthApi);

  const token = store.accessToken();
  const authed = token && !isAuthFree(req.url) ? withToken(req, token) : req;

  return next(authed).pipe(
    catchError((error: unknown) => {
      const is401 = error instanceof HttpErrorResponse && error.status === 401;
      const refresh = store.refreshTokenValue();

      if (!is401 || isAuthFree(req.url) || req.headers.has(RETRY_HEADER) || !refresh) {
        return throwError(() => error);
      }

      inflight ??= api.refresh(refresh).pipe(
        switchMap((tokens) => {
          store.setSession(tokens);
          return [tokens];
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
        finalize(() => (inflight = null)),
      );

      return inflight.pipe(
        switchMap((tokens) => {
          const retried = withToken(req, tokens.accessToken).clone({ setHeaders: { [RETRY_HEADER]: '1' } });
          return next(retried);
        }),
        catchError((refreshError: unknown) => {
          if (isSessionRejected(refreshError)) store.clear();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
