import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import {
  AdjustStockRequest, CreateInventoryItemRequest, InventoryItem, Paged, ReceiveStockRequest,
  SetThresholdRequest, StockMovement,
} from './models';

/** Inventory management (permission: inventory.manage). */
@Injectable({ providedIn: 'root' })
export class InventoryAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/inventory';

  create(request: CreateInventoryItemRequest): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(`${this.base}/items`, request);
  }
  list(lowStockOnly = false, page = 1, pageSize = 20): Observable<Paged<InventoryItem>> {
    const params = new HttpParams().set('lowStockOnly', lowStockOnly).set('page', page).set('pageSize', pageSize);
    return this.http.get<Paged<InventoryItem>>(`${this.base}/items`, { params });
  }
  get(sku: string): Observable<InventoryItem> { return this.http.get<InventoryItem>(`${this.base}/items/${encodeURIComponent(sku)}`); }
  movements(sku: string, page = 1, pageSize = 20): Observable<Paged<StockMovement>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<Paged<StockMovement>>(`${this.base}/items/${encodeURIComponent(sku)}/movements`, { params });
  }
  receive(sku: string, request: ReceiveStockRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/items/${encodeURIComponent(sku)}/receive`, request);
  }
  adjust(sku: string, request: AdjustStockRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/items/${encodeURIComponent(sku)}/adjust`, request);
  }
  setThreshold(sku: string, request: SetThresholdRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/items/${encodeURIComponent(sku)}/threshold`, request);
  }
}
