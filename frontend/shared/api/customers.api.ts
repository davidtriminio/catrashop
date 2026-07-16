import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { Address, AddressRequest, CustomerProfile, UpdateProfileRequest, WishlistItem } from './models';

@Injectable({ providedIn: 'root' })
export class CustomersApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/customers/me';

  profile(): Observable<CustomerProfile> { return this.http.get<CustomerProfile>(this.base); }
  updateProfile(request: UpdateProfileRequest): Observable<CustomerProfile> { return this.http.put<CustomerProfile>(this.base, request); }

  addresses(): Observable<Address[]> { return this.http.get<Address[]>(`${this.base}/addresses`); }
  addAddress(request: AddressRequest): Observable<Address> { return this.http.post<Address>(`${this.base}/addresses`, request); }
  updateAddress(id: string, request: AddressRequest): Observable<Address> { return this.http.put<Address>(`${this.base}/addresses/${id}`, request); }
  removeAddress(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/addresses/${id}`); }
  setDefaultShipping(id: string): Observable<void> { return this.http.put<void>(`${this.base}/addresses/${id}/default-shipping`, {}); }
  setDefaultBilling(id: string): Observable<void> { return this.http.put<void>(`${this.base}/addresses/${id}/default-billing`, {}); }

  wishlist(): Observable<WishlistItem[]> { return this.http.get<WishlistItem[]>(`${this.base}/wishlist`); }
  addWishlist(productId: string): Observable<void> { return this.http.post<void>(`${this.base}/wishlist`, { productId }); }
  removeWishlist(productId: string): Observable<void> { return this.http.delete<void>(`${this.base}/wishlist/${productId}`); }
}
