import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { OrderDetail, OrderSummary, Paged } from './models';

/** Order fulfilment (permissions: orders.read_all to view, orders.manage to act). */
@Injectable({ providedIn: 'root' })
export class OrdersAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/orders/admin';

  search(status?: string, page = 1, pageSize = 20): Observable<Paged<OrderSummary>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<Paged<OrderSummary>>(this.base, { params });
  }
  get(id: string): Observable<OrderDetail> { return this.http.get<OrderDetail>(`${this.base}/${id}`); }
  process(id: string): Observable<void> { return this.http.post<void>(`${this.base}/${id}/process`, {}); }
  ship(id: string): Observable<void> { return this.http.post<void>(`${this.base}/${id}/ship`, {}); }
  deliver(id: string): Observable<void> { return this.http.post<void>(`${this.base}/${id}/deliver`, {}); }
  cancel(id: string, reason?: string): Observable<void> { return this.http.post<void>(`${this.base}/${id}/cancel`, { reason: reason ?? null }); }
}
