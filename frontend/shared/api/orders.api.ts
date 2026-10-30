import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import {
  CancelOrderRequest, CheckoutRequest, CheckoutResponse, DeliveryMode, OrderDetail, OrderSummary, Paged, PaymentMethodOptions,
} from './models';

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/orders';

  checkout(request: CheckoutRequest): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>(`${this.base}/checkout`, request);
  }
  /** The payment methods the delivery option allows (the server re-checks the choice at checkout). */
  paymentMethods(mode: DeliveryMode, departmentCode?: string | null): Observable<PaymentMethodOptions> {
    let params = new HttpParams().set('deliveryMode', mode);
    if (departmentCode) params = params.set('departmentCode', departmentCode);
    return this.http.get<PaymentMethodOptions>(`${this.base}/payment-methods`, { params });
  }
  listMine(page = 1, pageSize = 20): Observable<Paged<OrderSummary>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<Paged<OrderSummary>>(this.base, { params });
  }
  getMine(id: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`${this.base}/${id}`);
  }
  cancel(id: string, request: CancelOrderRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, request);
  }
}
