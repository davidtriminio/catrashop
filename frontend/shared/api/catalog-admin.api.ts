import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import { AddVariantRequest, CreateBrandRequest, CreateCategoryRequest, CreateProductRequest, CreatedResource } from './models';

/** Catalog management (permission: catalog.products.manage). */
@Injectable({ providedIn: 'root' })
export class CatalogAdminApi {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE_URL) + '/api/v1/catalog';

  createCategory(request: CreateCategoryRequest): Observable<CreatedResource> {
    return this.http.post<CreatedResource>(`${this.base}/categories`, request);
  }
  createBrand(request: CreateBrandRequest): Observable<CreatedResource> {
    return this.http.post<CreatedResource>(`${this.base}/brands`, request);
  }
  createProduct(request: CreateProductRequest): Observable<CreatedResource> {
    return this.http.post<CreatedResource>(`${this.base}/products`, request);
  }
  addVariant(productId: string, request: AddVariantRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/products/${productId}/variants`, request);
  }
  publish(productId: string): Observable<void> { return this.http.post<void>(`${this.base}/products/${productId}/publish`, {}); }
  archive(productId: string): Observable<void> { return this.http.post<void>(`${this.base}/products/${productId}/archive`, {}); }
}
