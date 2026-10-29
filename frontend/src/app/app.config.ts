import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { environment } from '../environments/environment';
import { API_BASE_URL } from '@ch/api/api-base-url';
import { authInterceptor } from '@ch/auth/auth.interceptor';
import { provideI18n } from '@ch/i18n/i18n';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    provideI18n(),
    { provide: API_BASE_URL, useValue: environment.apiBaseUrl },
  ],
};
