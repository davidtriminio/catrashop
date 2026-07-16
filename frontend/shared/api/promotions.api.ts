import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { CouponQuote } from './models';

@Injectable({ providedIn: 'root' })
export class PromotionsApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/promotions';

  quote(couponCode: string, subtotal: number, currency: string): Observable<CouponQuote> {
    return this.http.post<CouponQuote>(`${this.base}/quote`, { couponCode, subtotal, currency });
  }
}
