import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthStore } from '@ch/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { MoneyPipe } from '@ch/util/money.pipe';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-cart-page',
  imports: [FormsModule, RouterLink, MoneyPipe],
  template: `
    <h1>Your cart</h1>

    @if (!auth.isAuthenticated()) {
      <div class="card">
        <p>Please <a routerLink="/login">sign in</a> to view your cart.</p>
      </div>
    } @else if (cart.cart(); as c) {
      @if (c.items.length === 0) {
        <div class="card"><p class="muted">Your cart is empty.</p><a routerLink="/products" class="btn">Browse products</a></div>
      } @else {
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <div class="card">
          <table>
            <thead><tr><th>Product</th><th>Price</th><th>Qty</th><th class="right">Total</th><th></th></tr></thead>
            <tbody>
              @for (item of c.items; track item.id) {
                <tr>
                  <td><strong>{{ item.productName }}</strong><br /><span class="muted">{{ item.sku }}</span></td>
                  <td>{{ item.unitPrice | money: c.currency }}</td>
                  <td>
                    <input class="input qty" type="number" min="0" [ngModel]="item.quantity"
                           (change)="updateQty(item.id, $event)" />
                  </td>
                  <td class="right">{{ item.lineTotal | money: c.currency }}</td>
                  <td class="right"><button class="btn secondary sm" (click)="remove(item.id)">Remove</button></td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="summary card">
          <div class="row"><span class="spacer">Subtotal</span><strong>{{ c.subtotal | money: c.currency }}</strong></div>
          <p class="muted">Taxes and any discount are calculated at checkout.</p>
          <a routerLink="/checkout" class="btn block">Proceed to checkout</a>
        </div>
      }
    } @else {
      <p class="muted">Loading…</p>
    }
  `,
  styles: [`
    .qty { width: 72px; }
    .summary { max-width: 360px; margin-left: auto; margin-top: 1rem; }
  `],
})
export class CartPage implements OnInit {
  readonly auth = inject(AuthStore);
  readonly cart = inject(CartStore);
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.cart.refresh(); }

  updateQty(itemId: string, event: Event): void {
    const quantity = Number((event.target as HTMLInputElement).value);
    if (Number.isNaN(quantity) || quantity < 0) return;
    this.error.set(null);
    this.cart.updateQuantity(itemId, quantity).subscribe({
      error: (err) => { this.error.set(problemMessage(err)); this.cart.refresh(); },
    });
  }

  remove(itemId: string): void {
    this.error.set(null);
    this.cart.remove(itemId).subscribe({ error: (err) => this.error.set(problemMessage(err)) });
  }
}
