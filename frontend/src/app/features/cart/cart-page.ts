import { MAX_LINE_QUANTITY } from '@cs/util/quantity';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthStore } from '@cs/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { I18n } from '@cs/i18n/i18n';
import { MoneyPipe } from '@cs/util/money.pipe';
import { PluralPipe } from '@cs/util/plural.pipe';

@Component({
  selector: 'app-cart-page',
  imports: [RouterLink, MoneyPipe, PluralPipe, TranslocoPipe],
  template: `
    <div class="page-head"><h1>{{ 'cart.title' | transloco }}</h1></div>

    @if (!auth.isAuthenticated()) {
      <div class="card empty">
        <h2>{{ 'cart.signInTitle' | transloco }}</h2>
        <p>{{ 'cart.signInText' | transloco }}</p>
        <a routerLink="/login" class="btn">{{ 'nav.signIn' | transloco }}</a>
      </div>
    } @else if (cart.cart(); as c) {
      @if (c.items.length === 0) {
        <div class="card empty">
          <h2>{{ 'cart.emptyTitle' | transloco }}</h2>
          <p>{{ 'cart.emptyText' | transloco }}</p>
          <a routerLink="/products" class="btn">{{ 'cart.browse' | transloco }}</a>
        </div>
      } @else {
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <div class="layout">
          <div class="card flush items">
            @for (item of c.items; track item.id) {
              <div class="list-row line">
                <div class="thumb" aria-hidden="true">{{ item.productName.charAt(0) }}</div>
                <div class="what">
                  <strong>{{ item.productName }}</strong>
                  <span class="muted">{{ item.sku }} · {{ 'cart.each' | transloco: { price: (item.unitPrice | money: c.currency) } }}</span>
                  <button class="remove" type="button" (click)="remove(item.id)">{{ 'cart.remove' | transloco }}</button>
                </div>
                <div class="stepper">
                  <button type="button" [attr.aria-label]="'cart.decrease' | transloco: { name: item.productName }" (click)="setQty(item.id, item.quantity - 1)">−</button>
                  <span>{{ item.quantity }}</span>
                  <button type="button" [attr.aria-label]="'cart.increase' | transloco: { name: item.productName }" (click)="setQty(item.id, item.quantity + 1)" [disabled]="item.quantity >= maxQuantity">+</button>
                </div>
                <strong class="total">{{ item.lineTotal | money: c.currency }}</strong>
              </div>
            }
          </div>

          <aside class="summary card">
            <h2>{{ 'cart.summary' | transloco }}</h2>
            <div class="row sum"><span class="spacer muted">{{ 'cart.subtotal' | transloco: { items: ('cart.items' | plural: c.itemCount) } }}</span><strong>{{ c.subtotal | money: c.currency }}</strong></div>
            <p class="muted small">{{ 'cart.taxesNote' | transloco }}</p>
            <a routerLink="/checkout" class="btn block">{{ 'cart.checkout' | transloco }}</a>
            <a routerLink="/products" class="continue">{{ 'cart.continue' | transloco }}</a>
          </aside>
        </div>
      }
    } @else {
      <div class="card"><div class="skeleton line"></div><div class="skeleton line short"></div></div>
    }
  `,
  styles: [`
    .layout { display: grid; grid-template-columns: minmax(0, 1fr) 340px; gap: var(--space-5); align-items: start; }
    .line { display: grid; grid-template-columns: 56px minmax(0, 1fr) auto 90px; align-items: center; gap: var(--space-4); }
    .thumb { width: 56px; height: 56px; border-radius: var(--radius-sm); background: #ece9f4; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 1.4rem; color: rgba(11, 11, 12, 0.25); }
    .what { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
    .what .muted { font-size: 0.82rem; }
    .remove { align-self: flex-start; background: none; border: none; padding: 0; margin-top: var(--space-1); font: inherit; font-size: 0.8rem; color: var(--muted); text-decoration: underline; text-underline-offset: 3px; cursor: pointer; }
    .remove:hover { color: var(--danger); }
    .stepper { display: inline-flex; align-items: center; border: 1px solid var(--border-strong); border-radius: var(--radius-pill); }
    .stepper button { width: 2.1rem; height: 2.1rem; border: none; background: none; font-size: 1.05rem; cursor: pointer; border-radius: var(--radius-pill); }
    .stepper button:hover { background: var(--bg); }
    .stepper span { min-width: 1.8rem; text-align: center; font-weight: 650; font-size: 0.9rem; }
    .total { text-align: right; }
    .summary { position: sticky; top: 90px; }
    .sum { font-size: 1rem; margin-bottom: var(--space-2); }
    .small { font-size: 0.82rem; margin-bottom: var(--space-4); }
    .continue { display: block; text-align: center; margin-top: var(--space-3); font-size: 0.85rem; color: var(--muted); }
    .empty .btn { margin-top: var(--space-2); }
    .empty h2 { color: var(--text); }
    .skeleton.line { height: 1.2rem; margin-bottom: var(--space-3); }
    .skeleton.line.short { width: 40%; }
    @media (max-width: 860px) {
      .layout { grid-template-columns: 1fr; }
      .summary { position: static; }
    }
    @media (max-width: 560px) {
      .line { grid-template-columns: 48px minmax(0, 1fr) auto; row-gap: var(--space-3); }
      .thumb { width: 48px; height: 48px; }
      .total { grid-column: 3; grid-row: 1; }
      .stepper { grid-column: 2 / 4; justify-self: start; }
    }
  `],
})
export class CartPage implements OnInit {
  private readonly i18n = inject(I18n);
  readonly auth = inject(AuthStore);
  readonly cart = inject(CartStore);
  readonly maxQuantity = MAX_LINE_QUANTITY;
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.cart.refresh(); }

  setQty(itemId: string, quantity: number): void {
    if (quantity < 0 || quantity > this.maxQuantity) return;
    this.error.set(null);
    this.cart.updateQuantity(itemId, quantity).subscribe({
      error: (err) => { this.error.set(this.i18n.problem(err)); this.cart.refresh(); },
    });
  }

  remove(itemId: string): void {
    this.error.set(null);
    this.cart.remove(itemId).subscribe({ error: (err) => this.error.set(this.i18n.problem(err)) });
  }
}
