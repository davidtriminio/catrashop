import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogApi } from '@ch/api/catalog.api';
import { CustomersApi } from '@ch/api/customers.api';
import { ProductDetail as ProductDetailModel, ProductVariant } from '@ch/api/models';
import { AuthStore } from '@ch/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { MoneyPipe } from '@ch/util/money.pipe';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-product-detail',
  imports: [RouterLink, MoneyPipe],
  template: `
    @if (loading()) {
      <div class="detail"><div class="skeleton media"></div><div class="info"><div class="skeleton line"></div><div class="skeleton line short"></div></div></div>
    }
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (product(); as p) {
      <nav class="crumbs" aria-label="Breadcrumb">
        <a routerLink="/">Home</a><span>/</span><a routerLink="/products">Shop</a><span>/</span><span aria-current="page">{{ p.name }}</span>
      </nav>

      <div class="detail">
        <div class="media card flush">
          @if (p.images.length) {
            <img [src]="p.images[0].url" [alt]="p.images[0].altText || p.name" />
          } @else {
            <div class="ph" aria-hidden="true"><span>{{ p.name.charAt(0) }}</span></div>
          }
        </div>

        <div class="info">
          <span class="eyebrow">{{ p.brandName || p.categoryName }}</span>
          <h1>{{ p.name }}</h1>
          <p class="muted">{{ p.categoryName }}</p>

          @if (selectedVariant(); as v) {
            <div class="price">{{ v.unitPrice | money: v.currency }}</div>
          }

          <p class="desc">{{ p.description }}</p>

          @if (activeVariants().length > 1) {
            <div class="group" role="radiogroup" aria-label="Option">
              <span class="label">Option</span>
              <div class="chips">
                @for (v of activeVariants(); track v.id) {
                  <button type="button" class="chip" role="radio" [class.active]="v.id === selectedVariantId()" [attr.aria-checked]="v.id === selectedVariantId()" (click)="selectedVariantId.set(v.id)">
                    {{ v.name }}
                  </button>
                }
              </div>
            </div>
          }

          <div class="group">
            <span class="label">Quantity</span>
            <div class="stepper">
              <button type="button" aria-label="Decrease quantity" (click)="step(-1)" [disabled]="quantity() <= 1">−</button>
              <span aria-live="polite">{{ quantity() }}</span>
              <button type="button" aria-label="Increase quantity" (click)="step(1)">+</button>
            </div>
          </div>

          @if (message()) { <div class="alert success">{{ message() }} <a routerLink="/cart">View cart</a></div> }
          @if (actionError()) { <div class="alert error">{{ actionError() }}</div> }

          <div class="buy">
            <button class="btn" [disabled]="!selectedVariantId() || busy()" (click)="addToCart()">Add to cart</button>
            @if (auth.isAuthenticated()) {
              <button class="btn secondary" [disabled]="busy()" (click)="addToWishlist(p.id)">♥ Wishlist</button>
            }
          </div>

          @if (p.attributes.length) {
            <div class="specs card">
              <h2>Details</h2>
              <dl>
                @for (a of p.attributes; track a.name) { <dt>{{ a.name }}</dt><dd>{{ a.value }}</dd> }
              </dl>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .crumbs { display: flex; flex-wrap: wrap; gap: var(--space-2); font-size: 0.82rem; color: var(--muted); margin-bottom: var(--space-4); }
    .crumbs a:hover { color: var(--text); }
    .detail { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: var(--space-6); align-items: start; }
    .media { position: sticky; top: 90px; background: #ece9f4; }
    .media img { width: 100%; aspect-ratio: 1 / 1; object-fit: cover; }
    .ph { aspect-ratio: 1 / 1; display: flex; align-items: center; justify-content: center; }
    .ph span { font-size: 8rem; font-weight: 700; color: rgba(11, 11, 12, 0.15); letter-spacing: -0.05em; }
    .eyebrow { font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.08em; color: var(--muted); }
    .info h1 { margin: var(--space-2) 0 var(--space-1); font-size: 2.1rem; }
    .price { font-size: 1.8rem; font-weight: 750; letter-spacing: -0.02em; margin: var(--space-4) 0; }
    .desc { color: #3f3f46; max-width: 52ch; margin-bottom: var(--space-5); }
    .group { margin-bottom: var(--space-5); }
    .label { display: block; font-size: 0.74rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); font-weight: 650; margin-bottom: var(--space-2); }
    .stepper { display: inline-flex; align-items: center; border: 1px solid var(--border-strong); border-radius: var(--radius-pill); background: var(--surface); }
    .stepper button { width: 2.5rem; height: 2.5rem; border: none; background: none; font-size: 1.15rem; cursor: pointer; border-radius: var(--radius-pill); }
    .stepper button:hover:not(:disabled) { background: var(--bg); }
    .stepper button:disabled { opacity: 0.35; cursor: not-allowed; }
    .stepper span { min-width: 2.2rem; text-align: center; font-weight: 650; }
    .buy { display: flex; gap: var(--space-3); flex-wrap: wrap; margin-bottom: var(--space-6); }
    .buy .btn:first-child { flex: 1; min-width: 180px; padding: 0.8rem 1.5rem; }
    .specs h2 { margin-bottom: var(--space-3); }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: var(--space-2) var(--space-5); margin: 0; }
    dt { color: var(--muted); }
    dd { margin: 0; font-weight: 550; }
    .skeleton.media { aspect-ratio: 1 / 1; }
    .skeleton.line { height: 1.2rem; margin-bottom: var(--space-3); }
    .skeleton.line.short { width: 40%; }
    @media (max-width: 760px) {
      .detail { grid-template-columns: 1fr; gap: var(--space-5); }
      .media { position: static; }
      .info h1 { font-size: 1.7rem; }
    }
  `],
})
export class ProductDetail implements OnInit {
  private readonly api = inject(CatalogApi);
  private readonly customers = inject(CustomersApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthStore);
  private readonly cart = inject(CartStore);

  readonly product = signal<ProductDetailModel | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly selectedVariantId = signal('');
  readonly quantity = signal(1);

  readonly activeVariants = computed<ProductVariant[]>(() => (this.product()?.variants ?? []).filter((v) => v.isActive));
  readonly selectedVariant = computed<ProductVariant | undefined>(() =>
    this.activeVariants().find((v) => v.id === this.selectedVariantId()));

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.loading.set(true);
    this.api.getProduct(slug).subscribe({
      next: (p) => {
        this.product.set(p);
        const first = p.variants.find((v) => v.isActive);
        if (first) this.selectedVariantId.set(first.id);
        this.loading.set(false);
      },
      error: (err) => { this.error.set(problemMessage(err, 'Product not found.')); this.loading.set(false); },
    });
  }

  step(delta: number): void {
    this.quantity.update((q) => Math.max(1, q + delta));
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
      next: () => { this.message.set('Added to your cart.'); this.busy.set(false); },
      error: (err) => { this.actionError.set(problemMessage(err, 'Could not add to cart.')); this.busy.set(false); },
    });
  }

  addToWishlist(productId: string): void {
    this.busy.set(true);
    this.actionError.set(null);
    this.customers.addWishlist(productId).subscribe({
      next: () => { this.message.set('Saved to your wishlist.'); this.busy.set(false); },
      error: (err) => { this.actionError.set(problemMessage(err, 'Could not update wishlist.')); this.busy.set(false); },
    });
  }
}
