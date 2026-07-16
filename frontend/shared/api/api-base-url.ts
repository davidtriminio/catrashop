import { InjectionToken } from '@angular/core';

/** Base origin for API calls (empty in dev — the dev server proxies "/api"). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
