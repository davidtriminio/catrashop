import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { Brand, Category, Paged, ProductDetail, ProductListItem } from './models';

export interface ProductQuery {
  q?: string; category?: string; brand?: string; sort?: string; page?: number; pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class CatalogApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/catalog';

  searchProducts(query: ProductQuery): Observable<Paged<ProductListItem>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') params = params.set(key, String(value));
    }
    return this.http.get<Paged<ProductListItem>>(`${this.base}/products`, { params });
  }
  getProduct(slug: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.base}/products/${encodeURIComponent(slug)}`);
  }
  listCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.base}/categories`);
  }
  listBrands(): Observable<Brand[]> {
    return this.http.get<Brand[]>(`${this.base}/brands`);
  }
}
