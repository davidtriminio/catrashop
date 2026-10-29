import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { API_BASE_URL } from '@ch/api/api-base-url';
import { authInterceptor } from '@ch/auth/auth.interceptor';
import { provideI18n } from '@ch/i18n/i18n';
import { environment } from '../environments/environment';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    // The admin's own texts are still English (translated in PR-049); only the shared badge/timeline follow the language.
    provideI18n({ defaultLang: 'en' }),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
  ],
};
