import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { CartApi } from '@cs/api/cart.api';
import { Cart } from '@cs/api/models';
import { AuthStore } from '@cs/auth/auth.store';

/** App-wide cart state so the header badge and cart page share one source of truth. */
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly api = inject(CartApi);
  private readonly auth = inject(AuthStore);

  readonly cart = signal<Cart | null>(null);
  readonly itemCount = computed(() => this.cart()?.itemCount ?? 0);
  readonly loading = signal(false);

  /** Load the cart if signed in (safe to call anytime; a no-op when anonymous). */
  refresh(): void {
    if (!this.auth.isAuthenticated()) { this.cart.set(null); return; }
    this.loading.set(true);
    this.api.get().subscribe({
      next: (c) => this.cart.set(c),
      error: () => this.cart.set(null),
      complete: () => this.loading.set(false),
    });
  }

  add(variantId: string, quantity = 1) {
    return this.api.addItem({ variantId, quantity }).pipe(tap((c) => this.cart.set(c)));
  }
  updateQuantity(itemId: string, quantity: number) {
    return this.api.updateItem(itemId, { quantity }).pipe(tap((c) => this.cart.set(c)));
  }
  remove(itemId: string) {
    return this.api.removeItem(itemId).pipe(tap((c) => this.cart.set(c)));
  }
  clearLocal(): void { this.cart.set(null); }
}
