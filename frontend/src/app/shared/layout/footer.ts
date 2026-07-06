import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `
    <footer class="site-footer">
      <div class="container">
        <span class="muted">CommerceHub — demo storefront. Prices and stock are illustrative.</span>
      </div>
    </footer>
  `,
  styles: [`
    .site-footer { border-top: 1px solid var(--border); margin-top: 2rem; padding: 1.5rem 0; background: var(--surface); }
  `],
})
export class Footer {}
