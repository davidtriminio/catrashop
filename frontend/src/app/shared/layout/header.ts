import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthStore } from '@ch/auth/auth.store';
import { LanguageSwitcher } from '@ch/ui/language-switcher';
import { CartStore } from '../../core/cart/cart.store';

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive, TranslocoPipe, LanguageSwitcher],
  template: `
    <header class="site-header">
      <div class="container bar">
        <a routerLink="/" class="brand">Catra<span>shop</span></a>

        <nav class="nav" [attr.aria-label]="'nav.main' | transloco">
          <a routerLink="/products" routerLinkActive="active">{{ 'nav.shop' | transloco }}</a>
          @if (auth.isAuthenticated()) {
            <a routerLink="/orders" routerLinkActive="active">{{ 'nav.orders' | transloco }}</a>
            <a routerLink="/account" routerLinkActive="active">{{ 'nav.account' | transloco }}</a>
            <a routerLink="/notifications" routerLinkActive="active">{{ 'nav.alerts' | transloco }}</a>
          }
        </nav>

        <div class="actions">
          <ch-language-switcher />
          <a routerLink="/cart" class="cart" routerLinkActive="active" [attr.aria-label]="'nav.cart' | transloco">
            <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M5 8h14l-1.2 11a1 1 0 0 1-1 .9H7.2a1 1 0 0 1-1-.9L5 8Z" /><path d="M9 8V7a3 3 0 0 1 6 0v1" />
            </svg>
            @if (cart.itemCount() > 0) { <span class="count">{{ cart.itemCount() }}</span> }
          </a>
          @if (auth.isAuthenticated()) {
            <span class="avatar" [title]="auth.user()?.firstName ?? ''">{{ initial() }}</span>
            <button class="btn secondary sm" (click)="logout()">{{ 'nav.signOut' | transloco }}</button>
          } @else {
            <a routerLink="/login" class="btn sm">{{ 'nav.signIn' | transloco }}</a>
          }
        </div>
      </div>
    </header>
  `,
  styles: [`
    .site-header { background: rgba(255, 255, 255, 0.92); backdrop-filter: blur(8px); border-bottom: 1px solid var(--border); position: sticky; top: 0; z-index: 10; }
    .bar { display: flex; align-items: center; gap: var(--space-5); padding-top: var(--space-3); padding-bottom: var(--space-3); }
    .brand { font-weight: 800; font-size: 1.2rem; letter-spacing: -0.03em; }
    .brand:hover { text-decoration: none; }
    .brand span { background: var(--accent); color: var(--accent-ink); padding: 0 0.3rem; border-radius: 6px; margin-left: 1px; }
    .nav { display: flex; gap: var(--space-4); flex: 1; }
    .nav a { color: var(--muted); font-weight: 600; font-size: 0.92rem; padding: var(--space-1) 0; border-bottom: 2px solid transparent; }
    .nav a:hover { color: var(--text); text-decoration: none; }
    .nav a.active { color: var(--text); border-bottom-color: var(--text); }
    .actions { display: flex; align-items: center; gap: var(--space-3); }
    .cart { position: relative; display: inline-flex; padding: var(--space-2); border-radius: var(--radius-pill); color: var(--text); }
    .cart:hover { background: var(--bg); text-decoration: none; }
    .count { position: absolute; top: -2px; right: -4px; background: var(--accent); color: var(--accent-ink); border-radius: var(--radius-pill); min-width: 1.1rem; padding: 0 0.3rem; font-size: 0.7rem; font-weight: 700; line-height: 1.1rem; text-align: center; }
    .avatar { width: 2rem; height: 2rem; border-radius: 50%; background: var(--primary); color: var(--primary-contrast); display: inline-flex; align-items: center; justify-content: center; font-weight: 700; font-size: 0.85rem; }

    /* Narrow screens: brand + actions on the first row, the menu scrolls horizontally on its own row. */
    @media (max-width: 720px) {
      .bar { flex-wrap: wrap; gap: var(--space-2) var(--space-3); }
      .brand { flex: 1; }
      .nav { order: 3; flex: 0 0 100%; overflow-x: auto; gap: var(--space-5); scrollbar-width: none; padding-bottom: var(--space-1); }
      .nav::-webkit-scrollbar { display: none; }
      .nav a { white-space: nowrap; }
    }
  `],
})
export class Header {
  readonly auth = inject(AuthStore);
  readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  initial(): string {
    return (this.auth.user()?.firstName ?? '?').charAt(0).toUpperCase();
  }

  logout(): void {
    this.auth.logout();
    this.cart.clearLocal();
    this.router.navigateByUrl('/products');
  }
}
