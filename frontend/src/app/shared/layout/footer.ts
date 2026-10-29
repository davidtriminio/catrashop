import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-footer',
  imports: [RouterLink, TranslocoPipe],
  template: `
    <footer class="site-footer">
      <div class="container foot">
        <div class="about">
          <span class="brand">Commerce<span>Hub</span></span>
          <p class="muted">{{ 'footer.about' | transloco }}</p>
        </div>
        <nav class="links" [attr.aria-label]="'footer.label' | transloco">
          <a routerLink="/products">{{ 'nav.shop' | transloco }}</a>
          <a routerLink="/orders">{{ 'footer.yourOrders' | transloco }}</a>
          <a routerLink="/track">{{ 'footer.track' | transloco }}</a>
          <a routerLink="/account">{{ 'nav.account' | transloco }}</a>
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
