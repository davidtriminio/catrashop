import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogApi } from '../../core/api/catalog.api';
import { CustomersApi } from '../../core/api/customers.api';
import { ProductDetail as ProductDetailModel, ProductVariant } from '../../core/api/models';
import { AuthStore } from '../../core/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { MoneyPipe } from '../../shared/money.pipe';
import { problemMessage } from '../../shared/problem';

@Component({
  selector: 'app-product-detail',
  imports: [FormsModule, RouterLink, MoneyPipe],
  template: `
    @if (loading()) { <p class="muted">Loading…</p> }
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (product(); as p) {
      <a routerLink="/products" class="muted">← Back to shop</a>
      <div class="detail">
        <div class="gallery card">
          @if (p.images.length) {
            <img [src]="p.images[0].url" [alt]="p.images[0].altText || p.name" />
          } @else { <div class="ph">No image</div> }
        </div>

        <div class="info">
          <h1>{{ p.name }}</h1>
          <p class="muted">{{ p.categoryName }}@if (p.brandName) { · {{ p.brandName }} }</p>
          <p>{{ p.description }}</p>

          @if (selectedVariant(); as v) {
            <div class="price">{{ v.unitPrice | money: v.currency }}</div>
          }

          <div class="field">
            <label for="variant">Option</label>
            <select id="variant" [(ngModel)]="selectedVariantId">
              @for (v of activeVariants(); track v.id) {
                <option [value]="v.id">{{ v.name }} — {{ v.unitPrice | money: v.currency }}</option>
              }
            </select>
          </div>

          <div class="field" style="max-width:120px">
            <label for="qty">Quantity</label>
            <input id="qty" class="input" type="number" min="1" [(ngModel)]="quantity" />
          </div>

          @if (message()) { <div class="alert success">{{ message() }}</div> }
          @if (actionError()) { <div class="alert error">{{ actionError() }}</div> }

          <div class="row">
            <button class="btn" [disabled]="!selectedVariantId || busy()" (click)="addToCart()">Add to cart</button>
            @if (auth.isAuthenticated()) {
              <button class="btn secondary" [disabled]="busy()" (click)="addToWishlist(p.id)">♥ Wishlist</button>
            }
          </div>

          @if (p.attributes.length) {
            <table class="attrs">
              <tbody>
                @for (a of p.attributes; track a.name) {
                  <tr><th>{{ a.name }}</th><td>{{ a.value }}</td></tr>
                }
              </tbody>
            </table>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .detail { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; margin-top: 1rem; }
    .gallery { padding: 0; overflow: hidden; }
    .gallery img { width: 100%; object-fit: cover; }
    .ph { aspect-ratio: 1; display: flex; align-items: center; justify-content: center; color: var(--muted); }
    .price { font-size: 1.6rem; font-weight: 800; margin: 0.5rem 0; }
    .attrs { margin-top: 1.5rem; }
    @media (max-width: 720px) { .detail { grid-template-columns: 1fr; } }
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

  selectedVariantId = '';
  quantity = 1;

  readonly activeVariants = computed<ProductVariant[]>(() => (this.product()?.variants ?? []).filter((v) => v.isActive));
  readonly selectedVariant = computed<ProductVariant | undefined>(() =>
    this.activeVariants().find((v) => v.id === this.selectedVariantId));

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.loading.set(true);
    this.api.getProduct(slug).subscribe({
      next: (p) => {
        this.product.set(p);
        const first = p.variants.find((v) => v.isActive);
        if (first) this.selectedVariantId = first.id;
        this.loading.set(false);
      },
      error: (err) => { this.error.set(problemMessage(err, 'Product not found.')); this.loading.set(false); },
    });
  }

  addToCart(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    this.busy.set(true);
    this.actionError.set(null);
    this.message.set(null);
    this.cart.add(this.selectedVariantId, Math.max(1, this.quantity)).subscribe({
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
