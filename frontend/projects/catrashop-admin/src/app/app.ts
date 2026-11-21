import { Component, computed, inject, signal } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthStore } from '@cs/auth/auth.store';
import { ADMIN_NAV } from './admin-permissions';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { LanguageSwitcher } from '@cs/ui/language-switcher';
import { Icon } from './ui/icon';
import { ThemeService } from './ui/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon, TranslocoPipe, LanguageSwitcher],
  template: `
    @if (navigating()) { <div class="route-bar" aria-hidden="true"></div> }
    @if (auth.isAuthenticated()) {
      <div class="shell">
        @if (menuOpen()) { <div class="backdrop" (click)="menuOpen.set(false)"></div> }
        <aside class="sidebar" [class.open]="menuOpen()" [attr.aria-label]="'admin.shell.main' | transloco">
          <a class="brand" routerLink="/dashboard">
            <span class="logo"><cs-icon name="bag" [size]="18" /></span>
            <span class="name">Catrashop<small>Admin</small></span>
          </a>
          <p class="group">{{ 'admin.shell.menu' | transloco }}</p>
          <nav>
            @for (item of visibleNav(); track item.path) {
              <a [routerLink]="item.path" routerLinkActive="active" ariaCurrentWhenActive="page">
                <cs-icon [name]="item.icon" [size]="18" /><span>{{ item.labelKey | transloco }}</span>
              </a>
            }
          </nav>
          <div class="me">
            <span class="avatar" aria-hidden="true">{{ initials() }}</span>
            <span class="who">
              <strong>{{ auth.user()?.firstName || 'Admin' }}</strong>
              <small>{{ auth.user()?.email }}</small>
            </span>
            <button type="button" class="icon-btn" [title]="'admin.shell.signOut' | transloco" [attr.aria-label]="'admin.shell.signOut' | transloco" (click)="logout()">
              <cs-icon name="logout" [size]="18" />
            </button>
          </div>
        </aside>

        <div class="main">
          <header class="topbar">
            <button type="button" class="icon-btn menu" [attr.aria-label]="'admin.shell.openMenu' | transloco" (click)="menuOpen.set(true)">
              <cs-icon name="menu" />
            </button>
            <span class="spacer"></span>
            <cs-language-switcher />
            <button type="button" class="icon-btn theme" (click)="theme.cycle()"
                    [title]="themeLabel()" [attr.aria-label]="themeLabel()">
              <cs-icon [name]="theme.mode() === 'system' ? 'monitor' : theme.mode() === 'dark' ? 'moon' : 'sun'" />
              <span class="mode">{{ 'admin.shell.mode.' + theme.mode() | transloco }}</span>
            </button>
          </header>
          <div class="content"><router-outlet /></div>
        </div>
      </div>
    } @else {
      <div class="float-tools"><cs-language-switcher /><button type="button" class="icon-btn" (click)="theme.cycle()"
              [title]="themeLabel()" [attr.aria-label]="themeLabel()">
        <cs-icon [name]="theme.mode() === 'system' ? 'monitor' : theme.mode() === 'dark' ? 'moon' : 'sun'" />
      </button></div>
      <router-outlet />
    }
  `,
  styles: [`
    .shell { display: grid; grid-template-columns: 252px minmax(0, 1fr); min-height: 100vh; }
    .sidebar {
      position: sticky; top: 0; height: 100vh; display: flex; flex-direction: column;
      background: var(--sidebar-bg); color: var(--sidebar-text); padding: var(--space-4) var(--space-3);
    }
    .brand { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-2) var(--space-3) var(--space-5); color: var(--sidebar-strong); }
    .brand:hover { text-decoration: none; }
    .logo {
      display: grid; place-items: center; width: 34px; height: 34px; border-radius: 10px;
      background: linear-gradient(135deg, #3d6bff, #003399); color: #fff; box-shadow: 0 4px 12px rgba(0, 51, 153, 0.45);
    }
    .name { font-weight: 800; font-size: 1.05rem; letter-spacing: -0.02em; line-height: 1.1; display: flex; flex-direction: column; }
    .name small { font-size: 0.62rem; font-weight: 700; letter-spacing: 0.22em; text-transform: uppercase; color: var(--sidebar-accent); margin-top: 2px; }
    .group { margin: 0 var(--space-3) var(--space-2); font-size: 0.68rem; font-weight: 700; letter-spacing: 0.12em; text-transform: uppercase; color: rgba(170, 182, 214, 0.55); }
    nav { display: flex; flex-direction: column; gap: 2px; overflow-y: auto; flex: 1; }
    nav a {
      position: relative; display: flex; align-items: center; gap: var(--space-3);
      color: var(--sidebar-text); padding: 0.6rem var(--space-3); border-radius: var(--radius-sm);
      font-weight: 550; font-size: 0.92rem; transition: background 0.15s, color 0.15s;
    }
    nav a:hover { background: var(--sidebar-hover); color: var(--sidebar-strong); text-decoration: none; }
    nav a.active { background: var(--sidebar-active); color: var(--sidebar-strong); }
    nav a.active::before { content: ''; position: absolute; left: -12px; top: 8px; bottom: 8px; width: 3px; border-radius: 0 3px 3px 0; background: var(--sidebar-accent); }
    nav a.active cs-icon { color: var(--sidebar-accent); }
    nav a:focus-visible, .icon-btn:focus-visible { outline: 2px solid var(--sidebar-accent); outline-offset: 2px; }
    .me { display: flex; align-items: center; gap: var(--space-3); margin-top: var(--space-3); padding: var(--space-3) var(--space-2) var(--space-1); border-top: 1px solid var(--sidebar-line); }
    .avatar { display: grid; place-items: center; width: 36px; height: 36px; border-radius: 50%; flex-shrink: 0; background: var(--sidebar-active); color: var(--sidebar-accent); font-weight: 700; font-size: 0.8rem; }
    .who { display: flex; flex-direction: column; min-width: 0; flex: 1; }
    .who strong { color: var(--sidebar-strong); font-size: 0.88rem; font-weight: 650; }
    .who small { font-size: 0.74rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .me .icon-btn { color: var(--sidebar-text); }
    .me .icon-btn:hover { background: var(--sidebar-hover); color: var(--sidebar-strong); }

    .main { display: flex; flex-direction: column; min-width: 0; }
    .topbar {
      position: sticky; top: 0; z-index: 5; display: flex; align-items: center; gap: var(--space-3);
      height: 60px; padding: 0 var(--space-5); background: color-mix(in srgb, var(--bg) 86%, transparent);
      backdrop-filter: blur(10px); border-bottom: 1px solid var(--border);
    }
    .content { padding: var(--space-5); width: 100%; max-width: var(--maxw); margin: 0 auto; }

    .icon-btn {
      display: inline-flex; align-items: center; gap: var(--space-2); border: 1px solid transparent; background: transparent;
      color: var(--muted); height: 36px; min-width: 36px; justify-content: center; padding: 0 var(--space-2);
      border-radius: var(--radius-sm); cursor: pointer; font: inherit; font-size: 0.8rem; font-weight: 600;
    }
    .icon-btn:hover { background: var(--surface); border-color: var(--border); color: var(--text); }
    .menu, .backdrop { display: none; }
    .float-tools { position: fixed; top: var(--space-4); right: var(--space-4); z-index: 10; display: flex; align-items: center; gap: var(--space-2); }
    .float-tools .icon-btn { background: var(--surface); border-color: var(--border); }

    @media (max-width: 900px) {
      .shell { grid-template-columns: 1fr; }
      .sidebar { position: fixed; z-index: 30; inset: 0 auto 0 0; width: 272px; transform: translateX(-100%); transition: transform 0.2s ease; }
      .sidebar.open { transform: none; box-shadow: var(--shadow-raised); }
      .backdrop { display: block; position: fixed; inset: 0; z-index: 20; background: rgba(5, 9, 22, 0.55); }
      .menu { display: inline-flex; }
      .topbar { padding: 0 var(--space-3); }
      .content { padding: var(--space-4) var(--space-3); }
    }
    @media (prefers-reduced-motion: reduce) { .sidebar { transition: none; } }
  `],
})
export class App {
  readonly auth = inject(AuthStore);
  readonly theme = inject(ThemeService);
  private readonly i18n = inject(I18n);

  readonly themeLabel = computed(() => {
    this.i18n.lang(); // recompute when the language changes
    return this.i18n.t('admin.shell.themeChange', { mode: this.i18n.t('admin.shell.mode.' + this.theme.mode()) });
  });
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);
  /** True once a page change has been running for a moment (fast ones never flash the bar). */
  readonly navigating = signal(false);
  private navTimer: ReturnType<typeof setTimeout> | undefined;

  readonly visibleNav = computed(() =>
    ADMIN_NAV.filter((item) => !item.permission || this.auth.hasPermission(item.permission)));

  readonly initials = computed(() => {
    const u = this.auth.user();
    const letters = `${u?.firstName?.[0] ?? ''}${u?.lastName?.[0] ?? ''}`;
    return (letters || u?.email?.[0] || 'A').toUpperCase();
  });

  constructor() {
    // Close the mobile drawer after navigating.
    this.router.events.pipe(takeUntilDestroyed()).subscribe((e) => {
      if (e instanceof NavigationStart) {
        clearTimeout(this.navTimer);
        this.navTimer = setTimeout(() => this.navigating.set(true), 150);
      } else if (e instanceof NavigationEnd || e instanceof NavigationCancel || e instanceof NavigationError) {
        clearTimeout(this.navTimer);
        this.navigating.set(false);
        if (e instanceof NavigationEnd) this.menuOpen.set(false);
      }
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
