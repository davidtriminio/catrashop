import { Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterOutlet } from '@angular/router';
import { CartStore } from './core/cart/cart.store';
import { Footer } from './shared/layout/footer';
import { Header } from './shared/layout/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Footer],
  template: `
    @if (navigating()) { <div class="route-bar" aria-hidden="true"></div> }
    <app-header />
    <main class="container">
      <router-outlet />
    </main>
    <app-footer />
  `,
})
export class App implements OnInit {
  private readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  /** True once a page change has been running for a moment (fast ones never flash the bar). */
  readonly navigating = signal(false);
  private navTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.router.events.pipe(takeUntilDestroyed()).subscribe((e) => {
      if (e instanceof NavigationStart) {
        clearTimeout(this.navTimer);
        this.navTimer = setTimeout(() => this.navigating.set(true), 150);
      } else if (e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError) {
        clearTimeout(this.navTimer);
        this.navigating.set(false);
      }
    });
  }

  ngOnInit(): void {
    this.cart.refresh();
  }
}
