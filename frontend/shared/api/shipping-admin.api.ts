import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { CreateShippingMethodRequest, ShippingMethod, UpdateShippingMethodRequest } from './models';

/** Shipping-method management (permission: orders.manage). */
@Injectable({ providedIn: 'root' })
export class ShippingAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/shipping/admin';

  createMethod(request: CreateShippingMethodRequest): Observable<ShippingMethod> {
    return this.http.post<ShippingMethod>(`${this.base}/methods`, request);
  }
  updateMethod(id: string, request: UpdateShippingMethodRequest): Observable<ShippingMethod> {
    return this.http.put<ShippingMethod>(`${this.base}/methods/${id}`, request);
  }
  activate(id: string): Observable<void> { return this.http.post<void>(`${this.base}/methods/${id}/activate`, {}); }
  deactivate(id: string): Observable<void> { return this.http.post<void>(`${this.base}/methods/${id}/deactivate`, {}); }
}
