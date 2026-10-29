import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import {
  CreateShippingMethodRequest, CreateShippingZoneRequest, PickupStore, SavePickupStoreRequest, ShippingMethod, ShippingZone,
  UpdateShippingMethodRequest, UpdateShippingZoneRequest,
} from './models';

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

  zones(): Observable<ShippingZone[]> { return this.http.get<ShippingZone[]>(`${this.base}/zones`); }
  createZone(request: CreateShippingZoneRequest): Observable<ShippingZone> { return this.http.post<ShippingZone>(`${this.base}/zones`, request); }
  updateZone(id: string, request: UpdateShippingZoneRequest): Observable<ShippingZone> { return this.http.put<ShippingZone>(`${this.base}/zones/${id}`, request); }
  setZoneActive(id: string, active: boolean): Observable<void> { return this.http.post<void>(`${this.base}/zones/${id}/${active ? 'activate' : 'deactivate'}`, {}); }

  pickupStores(): Observable<PickupStore[]> { return this.http.get<PickupStore[]>(`${this.base}/pickup-stores`); }
  createPickupStore(request: SavePickupStoreRequest): Observable<PickupStore> { return this.http.post<PickupStore>(`${this.base}/pickup-stores`, request); }
  updatePickupStore(id: string, request: SavePickupStoreRequest): Observable<PickupStore> { return this.http.put<PickupStore>(`${this.base}/pickup-stores/${id}`, request); }
  setPickupStoreActive(id: string, active: boolean): Observable<void> { return this.http.post<void>(`${this.base}/pickup-stores/${id}/${active ? 'activate' : 'deactivate'}`, {}); }
}
