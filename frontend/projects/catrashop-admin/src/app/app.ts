import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthStore } from '@cs/auth/auth.store';
import { ADMIN_NAV } from './admin-permissions';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    @if (auth.isAuthenticated()) {
      <div class="shell">
        <aside class="sidebar">
          <div class="brand">Catrashop<span>Admin</span></div>
          <nav>
            @for (item of visibleNav(); track item.path) {
              <a [routerLink]="item.path" routerLinkActive="active">{{ item.label }}</a>
            }
          </nav>
        </aside>
        <div class="main">
          <header class="topbar">
            <span class="spacer"></span>
            <span class="who">{{ auth.user()?.email }}</span>
            <button class="btn secondary sm" (click)="logout()">Sign out</button>
          </header>
          <div class="content"><router-outlet /></div>
        </div>
      </div>
    } @else {
      <router-outlet />
    }
  `,
  styles: [`
    .shell { display: grid; grid-template-columns: 240px 1fr; min-height: 100vh; }
    .sidebar { background: var(--primary); color: rgba(255, 255, 255, 0.82); padding: var(--space-5) var(--space-3); }
    .brand { font-weight: 800; font-size: 1.1rem; color: #fff; padding: 0 var(--space-3) var(--space-5); letter-spacing: -0.02em; }
    .brand span { display: block; font-size: 0.7rem; letter-spacing: 0.2em; color: var(--accent); text-transform: uppercase; }
    .sidebar nav { display: flex; flex-direction: column; gap: var(--space-1); }
    .sidebar nav a { color: rgba(255, 255, 255, 0.82); padding: 0.6rem var(--space-3); border-radius: var(--radius-sm); font-weight: 600; }
    .sidebar nav a:hover { background: rgba(255,255,255,0.08); text-decoration: none; }
    .sidebar nav a.active { background: var(--accent); color: var(--accent-ink); }
    .main { display: flex; flex-direction: column; }
    .topbar { display: flex; align-items: center; gap: 0.8rem; padding: 0.8rem 1.25rem; border-bottom: 1px solid var(--border); background: var(--surface); }
    .who { color: var(--muted); font-size: 0.9rem; }
    .content { padding: 1.5rem; }
  `],
})
export class App {
  readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  readonly visibleNav = computed(() =>
    ADMIN_NAV.filter((item) => !item.permission || this.auth.hasPermission(item.permission)));

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
