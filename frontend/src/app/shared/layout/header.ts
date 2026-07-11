import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthStore } from '../../core/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <header class="site-header">
      <div class="container bar">
        <a routerLink="/" class="brand">CommerceHub</a>
        <nav class="nav">
          <a routerLink="/products" routerLinkActive="active">Shop</a>
          @if (auth.isAuthenticated()) {
            <a routerLink="/orders" routerLinkActive="active">Orders</a>
            <a routerLink="/account" routerLinkActive="active">Account</a>
            <a routerLink="/notifications" routerLinkActive="active">Alerts</a>
          }
        </nav>
        <span class="spacer"></span>
        <a routerLink="/cart" class="cart" routerLinkActive="active">
          Cart
          @if (cart.itemCount() > 0) { <span class="count">{{ cart.itemCount() }}</span> }
        </a>
        @if (auth.isAuthenticated()) {
          <span class="who">{{ auth.user()?.firstName }}</span>
          <button class="btn secondary sm" (click)="logout()">Sign out</button>
        } @else {
          <a routerLink="/login" class="btn secondary sm">Sign in</a>
        }
      </div>
    </header>
  `,
  styles: [`
    .site-header { background: var(--surface); border-bottom: 1px solid var(--border); position: sticky; top: 0; z-index: 10; }
    .bar { display: flex; align-items: center; gap: 1rem; padding-top: 0.8rem; padding-bottom: 0.8rem; }
    .brand { font-weight: 800; font-size: 1.15rem; color: var(--text); }
    .brand:hover { text-decoration: none; color: var(--primary); }
    .nav { display: flex; gap: 1rem; }
    .nav a, .cart { color: var(--muted); font-weight: 600; }
    .nav a.active, .cart.active { color: var(--primary); }
    .cart { position: relative; }
    .count { background: var(--primary); color: #fff; border-radius: 999px; padding: 0 0.4rem; font-size: 0.72rem; margin-left: 0.2rem; }
    .who { color: var(--muted); font-size: 0.9rem; }
  `],
})
export class Header {
  readonly auth = inject(AuthStore);
  readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout();
    this.cart.clearLocal();
    this.router.navigateByUrl('/products');
  }
}
