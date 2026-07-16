import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { ShippingMethod, Tracking } from './models';

@Injectable({ providedIn: 'root' })
export class ShippingApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/shipping';

  methods(): Observable<ShippingMethod[]> { return this.http.get<ShippingMethod[]>(`${this.base}/methods`); }
  track(trackingNumber: string): Observable<Tracking> {
    return this.http.get<Tracking>(`${this.base}/track/${encodeURIComponent(trackingNumber)}`);
  }
  shipmentForOrder(orderId: string): Observable<Tracking> {
    return this.http.get<Tracking>(`${this.base}/orders/${orderId}/shipment`);
  }
}
