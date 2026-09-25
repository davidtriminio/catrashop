import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { InitiatePaymentRequest, PaymentResponse } from './models';

@Injectable({ providedIn: 'root' })
export class PaymentsApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/payments';

  /** Idempotent via the Idempotency-Key header; defaults to keying on the order id. */
  pay(request: InitiatePaymentRequest, idempotencyKey?: string): Observable<PaymentResponse> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey ?? `order:${request.orderId}` });
    return this.http.post<PaymentResponse>(this.base, request, { headers });
  }
  get(id: string): Observable<PaymentResponse> {
    return this.http.get<PaymentResponse>(`${this.base}/${id}`);
  }
}
