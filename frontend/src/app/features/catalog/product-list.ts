import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CatalogApi, ProductQuery } from '../../core/api/catalog.api';
import { Brand, Category, Paged, ProductListItem } from '../../core/api/models';
import { problemMessage } from '../../shared/problem';

@Component({
  selector: 'app-product-list',
  imports: [FormsModule, RouterLink],
  template: `
    <h1>Shop</h1>

    <div class="filters card">
      <input class="input" style="flex:2" placeholder="Search products…" [(ngModel)]="q" (keyup.enter)="applyFilters()" />
      <select [(ngModel)]="category" (change)="applyFilters()">
        <option value="">All categories</option>
        @for (c of categories(); track c.id) { <option [value]="c.slug">{{ c.name }}</option> }
      </select>
      <select [(ngModel)]="brand" (change)="applyFilters()">
        <option value="">All brands</option>
        @for (b of brands(); track b.id) { <option [value]="b.slug">{{ b.name }}</option> }
      </select>
      <select [(ngModel)]="sort" (change)="applyFilters()">
        <option value="">Sort</option>
        <option value="name">Name A–Z</option>
        <option value="name_desc">Name Z–A</option>
        <option value="newest">Newest</option>
      </select>
      <button class="btn" (click)="applyFilters()">Search</button>
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (loading()) { <p class="muted">Loading…</p> }

    @if (result(); as r) {
      @if (r.items.length === 0) {
        <p class="muted">No products match your search.</p>
      } @else {
        <div class="products">
          @for (p of r.items; track p.id) {
            <a class="product card" [routerLink]="['/products', p.slug]">
              <div class="thumb">
                @if (p.primaryImageUrl) { <img [src]="p.primaryImageUrl" [alt]="p.name" /> }
                @else { <div class="ph">No image</div> }
              </div>
              <div class="meta">
                <strong>{{ p.name }}</strong>
                <span class="muted">{{ p.categoryName }}@if (p.brandName) { · {{ p.brandName }} }</span>
              </div>
            </a>
          }
        </div>
        <div class="pager">
          <button class="btn secondary sm" [disabled]="!r.hasPrevious" (click)="go(r.page - 1)">Previous</button>
          <span class="muted">Page {{ r.page }} of {{ r.totalPages || 1 }}</span>
          <button class="btn secondary sm" [disabled]="!r.hasNext" (click)="go(r.page + 1)">Next</button>
        </div>
      }
    }
  `,
  styles: [`
    .filters { display: flex; gap: 0.6rem; flex-wrap: wrap; align-items: center; margin-bottom: 1rem; }
    .filters select, .filters .input { width: auto; }
    .products { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1rem; }
    .product { padding: 0; overflow: hidden; color: var(--text); }
    .product:hover { text-decoration: none; border-color: var(--primary); }
    .thumb { aspect-ratio: 4/3; background: #f1f1f4; display: flex; align-items: center; justify-content: center; }
    .thumb img { width: 100%; height: 100%; object-fit: cover; }
    .ph { color: var(--muted); font-size: 0.85rem; }
    .meta { padding: 0.8rem; display: flex; flex-direction: column; gap: 0.2rem; }
    .pager { display: flex; align-items: center; justify-content: center; gap: 1rem; margin: 1.5rem 0; }
  `],
})
export class ProductList implements OnInit {
  private readonly api = inject(CatalogApi);

  q = '';
  category = '';
  brand = '';
  sort = '';
  private page = 1;

  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly result = signal<Paged<ProductListItem> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.listCategories().subscribe({ next: (c) => this.categories.set(c), error: () => {} });
    this.api.listBrands().subscribe({ next: (b) => this.brands.set(b), error: () => {} });
    this.load();
  }

  applyFilters(): void { this.page = 1; this.load(); }
  go(page: number): void { this.page = page; this.load(); }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    const query: ProductQuery = { q: this.q, category: this.category, brand: this.brand, sort: this.sort, page: this.page, pageSize: 12 };
    this.api.searchProducts(query).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (err) => { this.error.set(problemMessage(err)); this.loading.set(false); },
    });
  }
}
