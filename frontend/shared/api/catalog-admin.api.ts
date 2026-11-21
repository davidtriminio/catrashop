import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url';
import {
  AddVariantRequest, AdminProductDetail, AdminProductImage, CreateBrandRequest, CreateCategoryRequest, CreateProductRequest,
  CreatedResource, Paged, ProductListItem, UpdateProductRequest, UpdateVariantRequest,
} from './models';

/** Catalog management (permission: catalog.products.manage). */
@Injectable({ providedIn: 'root' })
export class CatalogAdminApi {
  private readonly http = inject(HttpClient);
  private readonly origin = inject(API_BASE_URL);
  private readonly base = this.origin + '/api/v1/catalog';

  /** Uploaded photos are stored with a path relative to the API ("/api/v1/catalog/images/..."); this makes it loadable. */
  imageSrc(url: string): string { return url.startsWith('/') ? this.origin + url : url; }

  createCategory(request: CreateCategoryRequest): Observable<CreatedResource> {
    return this.http.post<CreatedResource>(`${this.base}/categories`, request);
  }
  createBrand(request: CreateBrandRequest): Observable<CreatedResource> {
    return this.http.post<CreatedResource>(`${this.base}/brands`, request);
  }
  createProduct(request: CreateProductRequest): Observable<CreatedResource> {
    return this.http.post<CreatedResource>(`${this.base}/products`, request);
  }

  /** Products in any status; `status` is Draft, Active or Archived (null for all). `q` matches name or SKU. */
  listProducts(status: string | null, q: string, page = 1, pageSize = 20): Observable<Paged<ProductListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (q.trim()) params = params.set('q', q.trim());
    return this.http.get<Paged<ProductListItem>>(`${this.base}/admin/products`, { params });
  }
  getProduct(id: string): Observable<AdminProductDetail> {
    return this.http.get<AdminProductDetail>(`${this.base}/admin/products/${id}`);
  }
  updateProduct(id: string, request: UpdateProductRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/products/${id}`, request);
  }
  addVariant(productId: string, request: AddVariantRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/products/${productId}/variants`, request);
  }
  updateVariant(productId: string, variantId: string, request: UpdateVariantRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/products/${productId}/variants/${variantId}`, request);
  }

  uploadImage(productId: string, file: File, altText?: string): Observable<AdminProductImage> {
    const body = new FormData();
    body.append('image', file);
    if (altText) body.append('altText', altText);
    return this.http.post<AdminProductImage>(`${this.base}/products/${productId}/images`, body);
  }
  setPrimaryImage(productId: string, imageId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/products/${productId}/images/${imageId}/primary`, {});
  }
  deleteImage(productId: string, imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/products/${productId}/images/${imageId}`);
  }

  /** Publishing also restores an archived product. */
  publish(productId: string): Observable<void> { return this.http.post<void>(`${this.base}/products/${productId}/publish`, {}); }
  archive(productId: string): Observable<void> { return this.http.post<void>(`${this.base}/products/${productId}/archive`, {}); }
}
