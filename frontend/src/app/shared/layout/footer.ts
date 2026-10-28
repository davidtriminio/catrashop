import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  template: `
    <footer class="site-footer">
      <div class="container foot">
        <div class="about">
          <span class="brand">Commerce<span>Hub</span></span>
          <p class="muted">A demo storefront. Prices and stock are illustrative.</p>
        </div>
        <nav class="links" aria-label="Footer">
          <a routerLink="/products">Shop</a>
          <a routerLink="/orders">Your orders</a>
          <a routerLink="/track">Track a shipment</a>
          <a routerLink="/account">Account</a>
        </nav>
      </div>
    </footer>
  `,
  styles: [`
    .site-footer { border-top: 1px solid var(--border); margin-top: var(--space-7); background: var(--surface); }
    .foot { display: flex; justify-content: space-between; align-items: flex-start; gap: var(--space-5); flex-wrap: wrap; padding-top: var(--space-6); padding-bottom: var(--space-6); }
    .brand { font-weight: 800; font-size: 1.1rem; letter-spacing: -0.03em; }
    .brand span { background: var(--accent); color: var(--accent-ink); padding: 0 0.3rem; border-radius: 6px; margin-left: 1px; }
    .about p { margin: var(--space-2) 0 0; font-size: 0.88rem; }
    .links { display: flex; gap: var(--space-5); flex-wrap: wrap; font-size: 0.9rem; font-weight: 500; }
    .links a { color: var(--muted); }
    .links a:hover { color: var(--text); }
  `],
})
export class Footer {}
