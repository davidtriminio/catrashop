import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { BankAccount, InitiatePaymentRequest, PaymentResponse } from './models';

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
  /** The payment of one of my orders (404 until the payment is started). */
  forOrder(orderId: string): Observable<PaymentResponse> {
    return this.http.get<PaymentResponse>(`${this.base}/by-order/${orderId}`);
  }
  bankAccounts(): Observable<BankAccount[]> {
    return this.http.get<BankAccount[]>(`${this.base}/bank-accounts`);
  }
  /** Attach the receipt (JPG, PNG or PDF up to 5 MB) of a bank transfer; an operator verifies it. */
  submitTransferProof(paymentId: string, bankAccountCode: string, reference: string, receipt: File): Observable<PaymentResponse> {
    const form = new FormData();
    form.append('bankAccountCode', bankAccountCode);
    form.append('reference', reference);
    form.append('receipt', receipt, receipt.name);
    return this.http.post<PaymentResponse>(`${this.base}/${paymentId}/transfer-proof`, form);
  }
}
