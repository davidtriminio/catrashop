import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, inject, provideAppInitializer } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { API_BASE_URL } from '@cs/api/api-base-url';
import { authInterceptor } from '@cs/auth/auth.interceptor';
import { SessionWatcher } from '@cs/auth/session-watcher';
import { provideI18n } from '@cs/i18n/i18n';
import { environment } from '../environments/environment';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    // Reload the stored session's profile and watch for the session ending (see SessionWatcher).
    provideAppInitializer(() => { inject(SessionWatcher).start(); }),
    provideI18n({ defaultLang: 'es' }),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
  ],
};
