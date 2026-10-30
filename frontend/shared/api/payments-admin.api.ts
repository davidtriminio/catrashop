import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { PaymentResponse } from './models';

/** Payment verification and collection (permission: orders.manage). */
@Injectable({ providedIn: 'root' })
export class PaymentsAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/payments/admin';

  list(status?: string, method?: string): Observable<PaymentResponse[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    if (method) params = params.set('method', method);
    return this.http.get<PaymentResponse[]>(this.base, { params });
  }
  forOrder(orderId: string): Observable<PaymentResponse> { return this.http.get<PaymentResponse>(`${this.base}/by-order/${orderId}`); }
  /** The receipt as a file (the request carries the bearer token, so it cannot be a plain link). */
  receipt(id: string): Observable<Blob> { return this.http.get(`${this.base}/${id}/receipt`, { responseType: 'blob' }); }
  approve(id: string): Observable<PaymentResponse> { return this.http.post<PaymentResponse>(`${this.base}/${id}/approve`, {}); }
  reject(id: string, reason: string): Observable<PaymentResponse> { return this.http.post<PaymentResponse>(`${this.base}/${id}/reject`, { reason }); }
  /** Cash on delivery / pay at the store: the money was collected (only after the order is delivered). */
  collect(id: string): Observable<PaymentResponse> { return this.http.post<PaymentResponse>(`${this.base}/${id}/collect`, {}); }
}
