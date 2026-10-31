import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { CatalogApi } from '@cs/api/catalog.api';
import { CustomersApi } from '@cs/api/customers.api';
import { ProductDetail as ProductDetailModel, ProductListItem, ProductVariant } from '@cs/api/models';
import { AuthStore } from '@cs/auth/auth.store';
import { I18n } from '@cs/i18n/i18n';
import { MoneyPipe } from '@cs/util/money.pipe';
import { MAX_LINE_QUANTITY, clampQuantity } from '@cs/util/quantity';
import { CartStore } from '../../core/cart/cart.store';

const RELATED_COUNT = 4;

@Component({
  selector: 'app-product-detail',
  imports: [RouterLink, MoneyPipe, TranslocoPipe],
  template: `
    @if (loading()) {
      <div class="top"><div class="skeleton gallery-skeleton"></div><div class="skeleton buy-skeleton"></div></div>
    }
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (product(); as p) {
      <nav class="crumbs" [attr.aria-label]="'orderDetail.breadcrumb' | transloco">
        <a routerLink="/">{{ 'catalog.home' | transloco }}</a><span>/</span><a routerLink="/products">{{ 'nav.shop' | transloco }}</a><span>/</span><span aria-current="page">{{ p.name }}</span>
      </nav>

      <div class="top">
        <div class="gallery">
          <div class="stage card flush">
            @if (currentImage(); as img) {
              <img [src]="img.url" [alt]="img.altText || p.name" />
            } @else {
              <div class="ph" aria-hidden="true"><span>{{ p.name.charAt(0) }}</span></div>
            }
          </div>
          @if (p.images.length > 1) {
            <div class="thumbs" role="tablist" [attr.aria-label]="'product.images' | transloco">
              @for (img of p.images; track img.url; let i = $index) {
                <button type="button" class="thumb" role="tab" [class.active]="i === imageIndex()" [attr.aria-selected]="i === imageIndex()" [attr.aria-label]="'product.image' | transloco: { n: i + 1 }" (click)="imageIndex.set(i)">
                  <img [src]="img.url" [alt]="''" loading="lazy" />
                </button>
              }
            </div>
          }
        </div>

        <aside class="buy-col">
          <div class="buy-box card">
            <span class="eyebrow">{{ p.brandName || p.categoryName }}</span>
            <h1>{{ p.name }}</h1>
            @if (p.brandName) { <p class="muted cat">{{ p.categoryName }}</p> }

            @if (selectedVariant(); as v) {
              <div class="price">{{ v.unitPrice | money: v.currency }}</div>
            }

            @if (activeVariants().length > 1) {
              <div class="group" role="radiogroup" [attr.aria-label]="'product.option' | transloco">
                <span class="label">{{ 'product.option' | transloco }}</span>
                <div class="chips">
                  @for (v of activeVariants(); track v.id) {
                    <button type="button" class="chip" role="radio" [class.active]="v.id === selectedVariantId()" [attr.aria-checked]="v.id === selectedVariantId()" (click)="selectedVariantId.set(v.id)">
                      {{ v.name }}
                    </button>
                  }
                </div>
              </div>
            }

            <div class="group qty">
              <span class="label">{{ 'product.quantity' | transloco }}</span>
              <div class="stepper">
                <button type="button" [attr.aria-label]="'product.decrease' | transloco" (click)="step(-1)" [disabled]="quantity() <= 1">−</button>
                <span aria-live="polite">{{ quantity() }}</span>
                <button type="button" [attr.aria-label]="'product.increase' | transloco" (click)="step(1)" [disabled]="quantity() >= maxQuantity">+</button>
              </div>
              @if (quantity() >= maxQuantity) { <span class="muted limit">{{ 'product.maxPerOrder' | transloco: { max: maxQuantity } }}</span> }
            </div>

            @if (message()) { <div class="alert success">{{ message() }} <a routerLink="/cart">{{ 'product.viewCart' | transloco }}</a></div> }
            @if (actionError()) { <div class="alert error">{{ actionError() }}</div> }

            <div class="buy">
              <button class="btn" [disabled]="!selectedVariantId() || busy()" (click)="addToCart()">{{ 'product.addToCart' | transloco }}</button>
              @if (auth.isAuthenticated()) {
                <button class="btn secondary wish" [class.on]="wishlisted()" [disabled]="busy()" [attr.aria-pressed]="wishlisted()" (click)="toggleWishlist(p.id)">
                  <svg viewBox="0 0 24 24" width="18" height="18" [attr.fill]="wishlisted() ? 'currentColor' : 'none'" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                    <path d="M19 14c1.49-1.46 3-3.21 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.76 0-3 .5-4.5 2-1.5-1.5-2.74-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4.05 3 5.5l7 7Z"/>
                  </svg>
                  {{ (wishlisted() ? 'product.wishlisted' : 'product.wishlist') | transloco }}
                </button>
              }
            </div>

            <ul class="perks">
              <li>{{ 'product.perkDelivery' | transloco }}</li>
              <li>{{ 'product.perkTrack' | transloco }}</li>
            </ul>
          </div>
        </aside>
      </div>

      <div class="sections">
        <section class="card about">
          <h2>{{ 'product.about' | transloco }}</h2>
          <p class="desc">{{ p.description }}</p>
        </section>

        <section class="card specs">
          <h2>{{ 'product.details' | transloco }}</h2>
          <dl>
            <dt>{{ 'catalog.category' | transloco }}</dt><dd>{{ p.categoryName }}</dd>
            @if (p.brandName) { <dt>{{ 'catalog.brand' | transloco }}</dt><dd>{{ p.brandName }}</dd> }
            @if (selectedVariant(); as v) { <dt>{{ 'product.sku' | transloco }}</dt><dd>{{ v.sku }}</dd> }
            @for (a of p.attributes; track a.name) { <dt>{{ a.name }}</dt><dd>{{ a.value }}</dd> }
          </dl>
        </section>

        <section class="card policies">
          <h2>{{ 'product.policiesTitle' | transloco }}</h2>
          <h3>{{ 'product.shippingTitle' | transloco }}</h3>
          <p>{{ 'product.shippingText' | transloco }}</p>
          <h3>{{ 'product.returnsTitle' | transloco }}</h3>
          <p>{{ 'product.returnsText' | transloco }}</p>
        </section>
      </div>

      @if (related().length) {
        <section class="related" aria-labelledby="related-title">
          <h2 id="related-title">{{ 'product.moreIn' | transloco: { category: p.categoryName } }}</h2>
          <div class="related-grid">
            @for (r of related(); track r.id) {
              <a class="rcard card flush" [routerLink]="['/products', r.slug]">
                <div class="rthumb">
                  @if (r.primaryImageUrl) { <img [src]="r.primaryImageUrl" [alt]="r.name" loading="lazy" /> }
                  @else { <span aria-hidden="true">{{ r.name.charAt(0) }}</span> }
                </div>
                <div class="rmeta">
                  <strong>{{ r.name }}</strong>
                  @if (r.minPrice !== null) {
                    <span class="rprice">
                      @if (r.maxPrice !== null && r.maxPrice !== r.minPrice) { <span class="muted">{{ 'catalog.from' | transloco }}</span> }
                      {{ r.minPrice | money: r.currency }}
                    </span>
                  }
                </div>
              </a>
            }
          </div>
        </section>
      }
    }
  `,
  styles: [`
    .crumbs { display: flex; flex-wrap: wrap; gap: var(--space-2); font-size: 0.82rem; color: var(--muted); margin-bottom: var(--space-4); }
    .crumbs a:hover { color: var(--text); }

    .top { display: grid; grid-template-columns: minmax(0, 480px) minmax(0, 1fr); gap: var(--space-6); align-items: start; justify-content: center; max-width: 960px; margin: 0 auto; }
    .gallery { min-width: 0; }
    .stage { background: var(--surface-2); }
    .stage img { display: block; width: 100%; aspect-ratio: 4 / 3; object-fit: contain; }
    .ph { aspect-ratio: 4 / 3; display: flex; align-items: center; justify-content: center; }
    .ph span { font-size: 5rem; font-weight: 700; color: var(--border-strong); letter-spacing: -0.05em; }
    .thumbs { display: flex; gap: var(--space-2); margin-top: var(--space-3); flex-wrap: wrap; }
    .thumb { width: 64px; height: 64px; padding: 0; border: 2px solid var(--border); border-radius: var(--radius-sm); background: var(--surface-2); cursor: pointer; overflow: hidden; }
    .thumb img { width: 100%; height: 100%; object-fit: cover; display: block; }
    .thumb:hover { border-color: var(--border-strong); }
    .thumb.active { border-color: var(--primary); }

    .buy-col { position: sticky; top: 90px; min-width: 0; }
    .buy-box { margin: 0; }
    .eyebrow { font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.08em; color: var(--muted); }
    .buy-box h1 { margin: var(--space-2) 0 var(--space-1); font-size: 1.7rem; line-height: 1.2; }
    .cat { margin: 0; font-size: 0.9rem; }
    .price { font-size: 1.7rem; font-weight: 750; letter-spacing: -0.02em; margin: var(--space-4) 0; }
    .group { margin-bottom: var(--space-4); }
    .label { display: block; font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); font-weight: 650; margin-bottom: var(--space-2); }
    .limit { display: block; font-size: 0.82rem; margin-top: var(--space-2); }
    .stepper { display: inline-flex; align-items: center; border: 1px solid var(--border-strong); border-radius: var(--radius-pill); background: var(--surface); }
    .stepper button { width: 2.5rem; height: 2.5rem; border: none; background: none; font-size: 1.15rem; cursor: pointer; border-radius: var(--radius-pill); }
    .stepper button:hover:not(:disabled) { background: var(--bg); }
    .stepper button:disabled { opacity: 0.35; cursor: not-allowed; }
    .stepper span { min-width: 2.2rem; text-align: center; font-weight: 650; }
    .btn.wish svg { transition: transform 0.15s; }
    .btn.wish.on { color: var(--primary); border-color: var(--primary); background: var(--tint-info-bg); }
    .btn.wish.on:hover { background: var(--tint-info-bg); }
    .btn.wish.on svg { transform: scale(1.1); }
    .buy { display: flex; gap: var(--space-3); flex-wrap: wrap; }
    .buy .btn:first-child { flex: 1; min-width: 160px; padding: 0.8rem 1.5rem; }
    .perks { list-style: none; margin: var(--space-4) 0 0; padding: var(--space-4) 0 0; border-top: 1px solid var(--border); display: grid; gap: var(--space-2); font-size: 0.86rem; color: var(--muted); }

    .sections { display: grid; grid-template-columns: minmax(0, 3fr) minmax(0, 2fr); gap: var(--space-4); max-width: 960px; margin: var(--space-6) auto 0; align-items: start; }
    .sections .card { margin: 0; }
    .about { grid-column: 1; }
    .specs { grid-column: 1; }
    .policies { grid-column: 2; grid-row: 1 / span 2; }
    .sections h2 { margin-bottom: var(--space-3); font-size: 1.1rem; }
    .policies h3 { font-size: 0.82rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); margin: var(--space-4) 0 var(--space-1); }
    .policies p { margin: 0; font-size: 0.92rem; }
    .desc { color: var(--tint-neutral-fg); margin: 0; white-space: pre-line; }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: var(--space-2) var(--space-5); margin: 0; }
    dt { color: var(--muted); }
    dd { margin: 0; font-weight: 550; overflow-wrap: anywhere; }

    .related { max-width: 960px; margin: var(--space-7) auto 0; }
    .related h2 { margin-bottom: var(--space-4); font-size: 1.2rem; }
    .related-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: var(--space-4); }
    .rcard { color: var(--text); display: flex; flex-direction: column; margin: 0; transition: border-color 0.15s, box-shadow 0.15s, transform 0.15s; }
    .rcard:hover { text-decoration: none; border-color: var(--border-strong); box-shadow: var(--shadow-raised); transform: translateY(-2px); }
    .rthumb { aspect-ratio: 4 / 3; background: var(--surface-2); display: flex; align-items: center; justify-content: center; overflow: hidden; }
    .rthumb img { width: 100%; height: 100%; object-fit: cover; }
    .rthumb span { font-size: 2.4rem; font-weight: 700; color: var(--border-strong); }
    .rmeta { padding: var(--space-3); display: flex; flex-direction: column; gap: var(--space-1); }
    .rmeta strong { font-size: 0.92rem; line-height: 1.3; overflow-wrap: anywhere; }
    .rprice { font-size: 0.9rem; font-weight: 650; }

    .gallery-skeleton { aspect-ratio: 4 / 3; border-radius: var(--radius); }
    .buy-skeleton { height: 22rem; border-radius: var(--radius); }

    @media (max-width: 900px) {
      .related-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    }
    @media (max-width: 760px) {
      .top { grid-template-columns: minmax(0, 1fr); gap: var(--space-5); }
      .buy-col { position: static; }
      .sections { grid-template-columns: minmax(0, 1fr); }
      .about, .specs, .policies { grid-column: 1; grid-row: auto; }
      .buy-box h1 { font-size: 1.45rem; }
      .related-grid { gap: var(--space-3); }
    }
  `],
})
export class ProductDetail {
  private readonly api = inject(CatalogApi);
  private readonly i18n = inject(I18n);
  private readonly customers = inject(CustomersApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthStore);
  private readonly cart = inject(CartStore);

  readonly product = signal<ProductDetailModel | null>(null);
  readonly related = signal<ProductListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  readonly wishlisted = signal(false);

  readonly selectedVariantId = signal('');
  readonly maxQuantity = MAX_LINE_QUANTITY;
  readonly quantity = signal(1);
  readonly imageIndex = signal(0);

  readonly activeVariants = computed<ProductVariant[]>(() => (this.product()?.variants ?? []).filter((v) => v.isActive));
  readonly selectedVariant = computed<ProductVariant | undefined>(() =>
    this.activeVariants().find((v) => v.id === this.selectedVariantId()));
  readonly currentImage = computed(() => {
    const images = this.product()?.images ?? [];
    return images[this.imageIndex()] ?? images[0] ?? null;
  });

  constructor() {
    // The route reuses this component when moving between related products, so follow the slug, not a snapshot.
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => this.load(params.get('slug')!));
  }

  private load(slug: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.product.set(null);
    this.related.set([]);
    this.quantity.set(1);
    this.imageIndex.set(0);
    this.message.set(null);
    this.actionError.set(null);
    this.wishlisted.set(false);
    this.api.getProduct(slug).subscribe({
      next: (p) => {
        this.product.set(p);
        const first = p.variants.find((v) => v.isActive);
        if (first) this.selectedVariantId.set(first.id);
        const primary = p.images.findIndex((i) => i.isPrimary);
        if (primary > 0) this.imageIndex.set(primary);
        this.loading.set(false);
        this.loadRelated(p);
        this.loadWishlisted(p.id);
      },
      error: (err) => { this.error.set(this.i18n.problem(err, 'product.notFound')); this.loading.set(false); },
    });
  }

  /** Other products of the same category; the category slug is resolved from the (small) category list. */
  private loadRelated(p: ProductDetailModel): void {
    this.api.listCategories().subscribe({
      next: (categories) => {
        const slug = categories.find((c) => c.id === p.categoryId)?.slug;
        if (!slug) return;
        this.api.searchProducts({ category: slug, pageSize: RELATED_COUNT + 1 }).subscribe({
          next: (page) => this.related.set(page.items.filter((i) => i.id !== p.id).slice(0, RELATED_COUNT)),
          error: () => this.related.set([]),
        });
      },
      error: () => this.related.set([]),
    });
  }

  step(delta: number): void {
    this.quantity.update((q) => clampQuantity(q + delta, this.maxQuantity));
  }

  addToCart(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    this.busy.set(true);
    this.actionError.set(null);
    this.message.set(null);
    this.cart.add(this.selectedVariantId(), this.quantity()).subscribe({
      next: () => { this.message.set(this.i18n.t('product.added')); this.busy.set(false); },
      error: (err) => { this.actionError.set(this.i18n.problem(err, 'product.addFailed')); this.busy.set(false); },
    });
  }

  /** Whether the signed-in customer already has this product in their wishlist, so the heart shows its real state. */
  private loadWishlisted(productId: string): void {
    if (!this.auth.isAuthenticated()) return;
    this.customers.wishlist().subscribe({
      next: (items) => this.wishlisted.set(items.some((i) => i.productId === productId)),
      error: () => {},
    });
  }

  toggleWishlist(productId: string): void {
    const adding = !this.wishlisted();
    this.busy.set(true);
    this.actionError.set(null);
    const request = adding ? this.customers.addWishlist(productId) : this.customers.removeWishlist(productId);
    request.subscribe({
      next: () => { this.wishlisted.set(adding); this.busy.set(false); },
      error: (err) => { this.actionError.set(this.i18n.problem(err, 'product.wishlistFailed')); this.busy.set(false); },
    });
  }
}
