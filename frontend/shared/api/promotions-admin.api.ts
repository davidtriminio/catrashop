import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { Coupon, CreateCouponRequest } from './models';

/** Coupon management (permission: promotions.manage). */
@Injectable({ providedIn: 'root' })
export class PromotionsAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/promotions/admin';

  list(): Observable<Coupon[]> { return this.http.get<Coupon[]>(`${this.base}/coupons`); }
  get(id: string): Observable<Coupon> { return this.http.get<Coupon>(`${this.base}/coupons/${id}`); }
  create(request: CreateCouponRequest): Observable<Coupon> { return this.http.post<Coupon>(`${this.base}/coupons`, request); }
  activate(id: string): Observable<void> { return this.http.post<void>(`${this.base}/coupons/${id}/activate`, {}); }
  deactivate(id: string): Observable<void> { return this.http.post<void>(`${this.base}/coupons/${id}/deactivate`, {}); }
}
