import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthStore } from '@ch/auth/auth.store';
import { ADMIN_NAV } from './admin-permissions';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    @if (auth.isAuthenticated()) {
      <div class="shell">
        <aside class="sidebar">
          <div class="brand">CommerceHub<span>Admin</span></div>
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
    .sidebar { background: #0b132b; color: #cbd5e1; padding: 1.2rem 0.8rem; }
    .brand { font-weight: 800; font-size: 1.1rem; color: #fff; padding: 0 0.6rem 1rem; }
    .brand span { display: block; font-size: 0.7rem; letter-spacing: 0.2em; color: #64ffda; text-transform: uppercase; }
    .sidebar nav { display: flex; flex-direction: column; gap: 0.15rem; }
    .sidebar nav a { color: #cbd5e1; padding: 0.55rem 0.6rem; border-radius: 8px; font-weight: 600; }
    .sidebar nav a:hover { background: rgba(255,255,255,0.06); text-decoration: none; }
    .sidebar nav a.active { background: var(--primary); color: #fff; }
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
