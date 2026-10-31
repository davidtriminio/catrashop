import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi, ProductQuery } from '@cs/api/catalog.api';
import { Brand, Category, Paged, PriceRange, ProductListItem } from '@cs/api/models';
import { I18n } from '@cs/i18n/i18n';
import { MoneyPipe } from '@cs/util/money.pipe';
import { PluralPipe } from '@cs/util/plural.pipe';
import { buildPages } from '@cs/util/pagination';

const PAGE_SIZE = 12;
/** Soft placeholder tints (used when a product has no photo); picked deterministically from the name. */
const TINTS = ['#e8efe9', '#ece9f4', '#f3ece6', '#e6eef3', '#f1e9ee', '#eef0e2'];

interface ActiveFilter { key: string; label: string; params: Record<string, null>; }

@Component({
  selector: 'app-product-list',
  imports: [RouterLink, MoneyPipe, PluralPipe, TranslocoPipe],
  template: `
    <nav class="crumbs" [attr.aria-label]="'orderDetail.breadcrumb' | transloco"><a routerLink="/">{{ 'catalog.home' | transloco }}</a><span>/</span><span aria-current="page">{{ 'nav.shop' | transloco }}</span></nav>

    <div class="page-head">
      <div>
        <h1>{{ 'nav.shop' | transloco }}</h1>
        <p class="muted count">
          @if (result(); as r) { {{ 'catalog.products' | plural: r.totalCount }} } @else { &nbsp; }
        </p>
      </div>
      <div class="toolbar">
        <button class="btn secondary sm filters-toggle" type="button" (click)="filtersOpen.set(!filtersOpen())" [attr.aria-expanded]="filtersOpen()">
          {{ 'catalog.filters' | transloco }}@if (activeFilters().length) { <span class="badge accent">{{ activeFilters().length }}</span> }
        </button>
        <label class="sort">
          <span class="muted">{{ 'catalog.sortBy' | transloco }}</span>
          <select [value]="sort()" (change)="update({ sort: value($event) || null })" [attr.aria-label]="'catalog.sortLabel' | transloco">
            <option value="">{{ 'catalog.sort.nameAsc' | transloco }}</option>
            <option value="name_desc">{{ 'catalog.sort.nameDesc' | transloco }}</option>
            <option value="price_asc">{{ 'catalog.sort.priceAsc' | transloco }}</option>
            <option value="price_desc">{{ 'catalog.sort.priceDesc' | transloco }}</option>
            <option value="newest">{{ 'catalog.sort.newest' | transloco }}</option>
          </select>
        </label>
      </div>
    </div>

    <div class="layout">
      <aside class="filters card" [class.open]="filtersOpen()" [attr.aria-label]="'catalog.filters' | transloco">
        <div class="f-head">
          <h2>{{ 'catalog.filters' | transloco }}</h2>
          @if (activeFilters().length) { <button class="link" type="button" (click)="clearAll()">{{ 'catalog.clearAll' | transloco }}</button> }
        </div>

        <section>
          <h3>{{ 'catalog.search' | transloco }}</h3>
          <input class="input" type="search" [placeholder]="'catalog.searchPlaceholder' | transloco" [value]="q()" (input)="onSearch($event)" [attr.aria-label]="'catalog.searchLabel' | transloco" />
        </section>

        <section>
          <h3>{{ 'catalog.category' | transloco }}</h3>
          <div class="options">
            <label class="opt"><input type="radio" name="category" [checked]="!category()" (change)="update({ category: null })" /> {{ 'catalog.allCategories' | transloco }}</label>
            @for (c of categories(); track c.id) {
              <label class="opt"><input type="radio" name="category" [checked]="category() === c.slug" (change)="update({ category: c.slug })" /> {{ c.name }}</label>
            }
          </div>
        </section>

        @if (brands().length) {
          <section>
            <h3>{{ 'catalog.brand' | transloco }}</h3>
            <div class="options">
              <label class="opt"><input type="radio" name="brand" [checked]="!brand()" (change)="update({ brand: null })" /> {{ 'catalog.allBrands' | transloco }}</label>
              @for (b of brands(); track b.id) {
                <label class="opt"><input type="radio" name="brand" [checked]="brand() === b.slug" (change)="update({ brand: b.slug })" /> {{ b.name }}</label>
              }
            </div>
          </section>
        }

        <section>
          <h3>{{ 'catalog.price' | transloco }}</h3>
          <div class="price">
            <input class="input" type="number" min="0" inputmode="decimal" [placeholder]="'catalog.min' | transloco" [value]="minPrice() ?? ''" (change)="setPrice('minPrice', $event)" [attr.aria-label]="'catalog.minLabel' | transloco" />
            <span class="muted">–</span>
            <input class="input" type="number" min="0" inputmode="decimal" [placeholder]="'catalog.max' | transloco" [value]="maxPrice() ?? ''" (change)="setPrice('maxPrice', $event)" [attr.aria-label]="'catalog.maxLabel' | transloco" />
          </div>
          @if (range(); as pr) {
            @if (pr.min !== null && pr.max !== null) {
              <p class="muted hint">{{ 'catalog.priceRange' | transloco: { min: (pr.min | money: pr.currency), max: (pr.max | money: pr.currency) } }}</p>
            }
          }
        </section>
      </aside>

      <section class="results" aria-live="polite">
        @if (activeFilters().length) {
          <div class="chips active">
            @for (f of activeFilters(); track f.key) {
              <button class="chip active" type="button" (click)="update(f.params)">{{ f.label }} <span aria-hidden="true">×</span><span class="sr">{{ 'catalog.removeFilter' | transloco }}</span></button>
            }
            <button class="link" type="button" (click)="clearAll()">{{ 'catalog.clearAll' | transloco }}</button>
          </div>
        }

        @if (error()) { <div class="alert error">{{ error() }}</div> }

        @if (loading() && !result()) {
          <div class="products">
            @for (n of skeletons; track n) { <div class="product card flush"><div class="skeleton thumb"></div><div class="meta"><div class="skeleton line"></div><div class="skeleton line short"></div></div></div> }
          </div>
        } @else if (result(); as r) {
          @if (r.items.length === 0) {
            <div class="empty card">
              <h2>{{ 'catalog.noMatchTitle' | transloco }}</h2>
              <p>{{ 'catalog.noMatchText' | transloco }}</p>
              @if (activeFilters().length) { <button class="btn" type="button" (click)="clearAll()">{{ 'catalog.clearFilters' | transloco }}</button> }
            </div>
          } @else {
            <div class="products" [class.dim]="loading()">
              @for (p of r.items; track p.id) {
                <a class="product card flush" [routerLink]="['/products', p.slug]">
                  <div class="thumb" [style.background]="p.primaryImageUrl ? null : tint(p.name)">
                    @if (p.primaryImageUrl) { <img [src]="p.primaryImageUrl" [alt]="p.name" loading="lazy" /> }
                    @else { <span class="mono" aria-hidden="true">{{ p.name.charAt(0) }}</span> }
                    @if (p.variantCount > 1) { <span class="badge accent variants">{{ 'catalog.options' | transloco: { count: p.variantCount } }}</span> }
                  </div>
                  <div class="meta">
                    <span class="eyebrow">{{ p.brandName || p.categoryName }}</span>
                    <strong class="name">{{ p.name }}</strong>
                    <span class="price-line">
                      @if (p.minPrice !== null) {
                        @if (p.maxPrice !== null && p.maxPrice !== p.minPrice) { <span class="muted from">{{ 'catalog.from' | transloco }}</span> }
                        {{ p.minPrice | money: p.currency }}
                      } @else { <span class="muted">{{ 'catalog.priceOnRequest' | transloco }}</span> }
                    </span>
                  </div>
                </a>
              }
            </div>

            @if (r.totalPages > 1) {
              <nav class="pager" [attr.aria-label]="'pagination.label' | transloco">
                <button class="btn secondary sm" type="button" [disabled]="!r.hasPrevious" (click)="goTo(r.page - 1)">{{ 'pagination.previous' | transloco }}</button>
                @for (item of pages(); track $index) {
                  @if (item === '…') { <span class="gap muted">…</span> }
                  @else { <button class="page" type="button" [class.current]="item === r.page" [attr.aria-current]="item === r.page ? 'page' : null" (click)="goTo(item)">{{ item }}</button> }
                }
                <button class="btn secondary sm" type="button" [disabled]="!r.hasNext" (click)="goTo(r.page + 1)">{{ 'pagination.next' | transloco }}</button>
              </nav>
            }
          }
        }
      </section>
    </div>
  `,
  styles: [`
    .crumbs { display: flex; gap: var(--space-2); font-size: 0.82rem; color: var(--muted); margin-bottom: var(--space-3); }
    .crumbs a:hover { color: var(--text); }
    .count { margin: var(--space-1) 0 0; min-height: 1.5em; }
    .toolbar { display: flex; align-items: center; gap: var(--space-3); }
    .sort { display: flex; align-items: center; gap: var(--space-2); font-size: 0.85rem; }
    .sort select { width: auto; padding: 0.45rem 0.75rem; border-radius: var(--radius-pill); }
    .filters-toggle { display: none; gap: var(--space-2); }

    .layout { display: grid; grid-template-columns: 260px 1fr; gap: var(--space-6); align-items: start; }
    .filters { position: sticky; top: 80px; display: flex; flex-direction: column; gap: var(--space-5); padding: var(--space-5); }
    .f-head { display: flex; align-items: center; justify-content: space-between; }
    .f-head h2 { margin: 0; }
    .filters section h3 { font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); margin-bottom: var(--space-3); }
    .options { display: flex; flex-direction: column; gap: var(--space-2); max-height: 220px; overflow: auto; }
    .opt { display: flex; align-items: center; gap: var(--space-3); font-size: 0.9rem; cursor: pointer; }
    .opt input { accent-color: var(--primary); width: 1rem; height: 1rem; margin: 0; }
    .price { display: flex; align-items: center; gap: var(--space-2); }
    .price .input { padding: 0.5rem 0.65rem; }
    .hint { font-size: 0.78rem; margin: var(--space-2) 0 0; }
    .link { background: none; border: none; padding: 0; font: inherit; font-size: 0.82rem; color: var(--muted); text-decoration: underline; text-underline-offset: 3px; cursor: pointer; }
    .link:hover { color: var(--text); }

    .active { margin-bottom: var(--space-4); align-items: center; }
    .chips.active { gap: var(--space-2) var(--space-3); }
    .chips.active .chip.active { background: var(--tint-info-bg); border-color: transparent; color: var(--tint-info-fg); font-weight: 600; box-sizing: border-box; height: 2rem; line-height: 1; margin: 0; padding: 0 0.45rem 0 0.85rem; }
    .chips.active .chip.active span[aria-hidden] { display: inline-flex; align-items: center; justify-content: center; width: 1.1rem; height: 1.1rem; border-radius: 50%; font-size: 0.85rem; line-height: 1; }
    .chips.active .chip.active:hover { background: var(--primary); color: var(--primary-contrast); }
    .chips.active .link { display: inline-flex; align-items: center; box-sizing: border-box; height: 2rem; line-height: 1; margin: 0; color: var(--primary); font-weight: 600; text-decoration: none; padding: 0 0.95rem; border: 1px solid var(--primary); border-radius: var(--radius-pill); background: var(--surface); transition: background 0.15s, color 0.15s; }
    .chips.active .link:hover { background: var(--primary); color: var(--primary-contrast); }
    .sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }

    .products { display: grid; grid-template-columns: repeat(auto-fill, minmax(210px, 1fr)); gap: var(--space-4); transition: opacity 0.15s; }
    .products.dim { opacity: 0.55; }
    .products > .card + .card { margin-top: 0; }
    .product { color: var(--text); display: flex; flex-direction: column; transition: border-color 0.15s, box-shadow 0.15s, transform 0.15s; }
    .product:hover { text-decoration: none; border-color: var(--border-strong); box-shadow: var(--shadow-raised); transform: translateY(-2px); }
    .thumb { position: relative; aspect-ratio: 1 / 1; display: flex; align-items: center; justify-content: center; }
    .thumb img { width: 100%; height: 100%; object-fit: cover; }
    .mono { font-size: 3.5rem; font-weight: 700; color: rgba(11, 11, 12, 0.18); letter-spacing: -0.04em; }
    .variants { position: absolute; left: var(--space-3); top: var(--space-3); }
    .meta { padding: var(--space-4); display: flex; flex-direction: column; gap: var(--space-1); }
    .eyebrow { font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.08em; color: var(--muted); }
    .name { font-size: 0.98rem; line-height: 1.3; }
    .price-line { margin-top: var(--space-2); font-weight: 650; }
    .from { font-weight: 400; margin-right: var(--space-1); }

    .skeleton.thumb { aspect-ratio: 1 / 1; border-radius: 0; }
    .skeleton.line { height: 0.9rem; margin-bottom: var(--space-2); }
    .skeleton.line.short { width: 55%; }

    .empty h2 { color: var(--text); }
    .empty .btn { margin-top: var(--space-3); }

    .pager { display: flex; align-items: center; justify-content: center; gap: var(--space-2); margin: var(--space-6) 0 var(--space-2); flex-wrap: wrap; }
    .page { min-width: 2.2rem; height: 2.2rem; border-radius: var(--radius-pill); border: 1px solid transparent; background: none; font: inherit; font-weight: 600; cursor: pointer; }
    .page:hover { border-color: var(--border-strong); }
    .page.current { background: var(--primary); color: var(--primary-contrast); }
    .gap { padding: 0 var(--space-1); }

    @media (max-width: 860px) {
      .layout { grid-template-columns: 1fr; gap: var(--space-4); }
      .filters { display: none; position: static; }
      .filters.open { display: flex; }
      .filters-toggle { display: inline-flex; }
      .sort span { display: none; }
      .page-head { align-items: flex-start; flex-direction: column; }
      .toolbar { width: 100%; justify-content: space-between; }
      .products { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-3); }
      .meta { padding: var(--space-3); }
    }
  `],
})
export class ProductList implements OnInit {
  private readonly api = inject(CatalogApi);
  private readonly i18n = inject(I18n);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  readonly skeletons = [1, 2, 3, 4, 5, 6];
  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly range = signal<PriceRange | null>(null);
  readonly result = signal<Paged<ProductListItem> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly filtersOpen = signal(false);

  // The filter state lives in the URL (shareable, back button works); these signals mirror it.
  readonly q = signal('');
  readonly category = signal('');
  readonly brand = signal('');
  readonly sort = signal('');
  readonly minPrice = signal<number | null>(null);
  readonly maxPrice = signal<number | null>(null);
  readonly page = signal(1);

  readonly pages = computed(() => buildPages(this.result()?.page ?? 1, this.result()?.totalPages ?? 0));

  readonly activeFilters = computed<ActiveFilter[]>(() => {
    this.i18n.lang(); // labels are translated: recompute when the language changes
    const filters: ActiveFilter[] = [];
    if (this.q()) filters.push({ key: 'q', label: `“${this.q()}”`, params: { q: null } });
    const category = this.categories().find((c) => c.slug === this.category());
    if (this.category()) filters.push({ key: 'category', label: category?.name ?? this.category(), params: { category: null } });
    const brand = this.brands().find((b) => b.slug === this.brand());
    if (this.brand()) filters.push({ key: 'brand', label: brand?.name ?? this.brand(), params: { brand: null } });
    if (this.minPrice() !== null || this.maxPrice() !== null) {
      const lo = this.minPrice() ?? 0;
      const hi = this.maxPrice();
      filters.push({ key: 'price', label: hi === null ? this.i18n.t('catalog.priceFrom', { min: lo }) : `${lo} – ${hi}`, params: { minPrice: null, maxPrice: null } });
    }
    return filters;
  });

  ngOnInit(): void {
    this.api.listCategories().subscribe({ next: (c) => this.categories.set(c), error: () => {} });
    this.api.listBrands().subscribe({ next: (b) => this.brands.set(b), error: () => {} });
    this.api.priceRange().subscribe({ next: (r) => this.range.set(r), error: () => {} });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.q.set(params.get('q') ?? '');
      this.category.set(params.get('category') ?? '');
      this.brand.set(params.get('brand') ?? '');
      this.sort.set(params.get('sort') ?? '');
      this.minPrice.set(toNumber(params.get('minPrice')));
      this.maxPrice.set(toNumber(params.get('maxPrice')));
      this.page.set(Math.max(1, Number(params.get('page')) || 1));
      this.load();
    });
  }

  value(event: Event): string { return (event.target as HTMLSelectElement).value; }

  tint(name: string): string {
    let hash = 0;
    for (const ch of name) hash = (hash * 31 + ch.charCodeAt(0)) >>> 0;
    return TINTS[hash % TINTS.length];
  }

  /** Changes filters in the URL; any change but pagination returns to page 1. */
  update(changes: Record<string, string | number | null>): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: null, ...changes },
      queryParamsHandling: 'merge',
    });
  }

  goTo(page: number | '…'): void {
    if (page === '…') return;
    this.update({ page: page === 1 ? null : page });
    if (typeof window !== 'undefined') window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  clearAll(): void {
    this.update({ q: null, category: null, brand: null, minPrice: null, maxPrice: null });
  }

  onSearch(event: Event): void {
    const text = (event.target as HTMLInputElement).value.trim();
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.update({ q: text || null }), 350);
  }

  setPrice(key: 'minPrice' | 'maxPrice', event: Event): void {
    const parsed = toNumber((event.target as HTMLInputElement).value);
    this.update({ [key]: parsed });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    const query: ProductQuery = {
      q: this.q(), category: this.category(), brand: this.brand(), sort: this.sort(),
      minPrice: this.minPrice() ?? undefined, maxPrice: this.maxPrice() ?? undefined,
      page: this.page(), pageSize: PAGE_SIZE,
    };
    this.api.searchProducts(query).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (err) => { this.error.set(this.i18n.problem(err)); this.loading.set(false); },
    });
  }
}

function toNumber(value: string | null): number | null {
  if (value === null || value.trim() === '') return null;
  const n = Number(value);
  return Number.isFinite(n) && n >= 0 ? n : null;
}
