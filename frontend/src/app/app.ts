import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CartStore } from './core/cart/cart.store';
import { Footer } from './shared/layout/footer';
import { Header } from './shared/layout/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer],
  template: `
    <app-header />
    <main class="container">
      <router-outlet />
    </main>
    <app-footer />
  `,
})
export class App implements OnInit {
  private readonly cart = inject(CartStore);

  ngOnInit(): void {
    this.cart.refresh();
  }
}
