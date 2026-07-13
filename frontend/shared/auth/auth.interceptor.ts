import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthApi } from '../api/auth.api';
import { AuthStore } from './auth.store';

const AUTH_FREE = ['/identity/login', '/identity/register', '/identity/refresh'];
const RETRY_HEADER = 'X-Auth-Retried';

function isAuthFree(url: string): boolean {
  return AUTH_FREE.some((path) => url.includes(path));
}

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

/**
 * Attaches the bearer token and, on a 401, transparently refreshes once (rotating the token) and retries the
 * original request. If the refresh itself fails, the session is cleared. Auth endpoints are exempt, and a
 * retry marker prevents an infinite refresh loop.
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

      return api.refresh(refresh).pipe(
        switchMap((tokens) => {
          store.setSession(tokens);
          const retried = withToken(req, tokens.accessToken).clone({ setHeaders: { [RETRY_HEADER]: '1' } });
          return next(retried);
        }),
        catchError((refreshError: unknown) => {
          store.clear();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
