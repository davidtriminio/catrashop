import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { AddCartItemRequest, Cart, UpdateCartItemRequest } from './models';

@Injectable({ providedIn: 'root' })
export class CartApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/cart';

  get(): Observable<Cart> { return this.http.get<Cart>(this.base); }
  addItem(request: AddCartItemRequest): Observable<Cart> { return this.http.post<Cart>(`${this.base}/items`, request); }
  updateItem(itemId: string, request: UpdateCartItemRequest): Observable<Cart> {
    return this.http.put<Cart>(`${this.base}/items/${itemId}`, request);
  }
  removeItem(itemId: string): Observable<Cart> { return this.http.delete<Cart>(`${this.base}/items/${itemId}`); }
  clear(): Observable<void> { return this.http.delete<void>(this.base); }
}
