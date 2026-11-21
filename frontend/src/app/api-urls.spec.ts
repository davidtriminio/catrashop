import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '@cs/api/api-base-url';
import { NotificationsApi } from '@cs/api/notifications.api';

/**
 * In production the storefront and the admin reach the API through Vercel's "/api/:path*" rewrite, which does not
 * match a path that ends in "/": that request falls through to index.html and comes back as HTML with status 200.
 */
describe('API URLs behind the Vercel rewrite', () => {
  it('the notifications list is requested without a trailing slash', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: API_BASE_URL, useValue: '' }],
    });
    TestBed.inject(NotificationsApi).list(false, 1, 50).subscribe();

    const req = TestBed.inject(HttpTestingController).expectOne((r) => r.url.startsWith('/api/v1/notifications'));
    expect(req.request.url).toBe('/api/v1/notifications');
    expect(req.request.url.endsWith('/')).toBe(false);
    req.flush({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0, hasPrevious: false, hasNext: false });
  });
});
