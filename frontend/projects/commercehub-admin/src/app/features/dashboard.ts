import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthStore } from '@ch/auth/auth.store';
import { ADMIN_NAV } from '../admin-permissions';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <h1>Welcome{{ auth.user() ? ', ' + auth.user()!.firstName : '' }}</h1>
    <p class="muted">Manage the storefront. You see only the sections your role permits.</p>

    <div class="cards">
      @for (item of sections(); track item.path) {
        <a class="card tile" [routerLink]="item.path">
          <strong>{{ item.label }}</strong>
          <span class="muted">Open {{ item.label.toLowerCase() }}</span>
        </a>
      }
    </div>

    @if (auth.user(); as u) {
      <div class="card">
        <h2>Your access</h2>
        <p class="muted">Roles: {{ u.roles.join(', ') || '—' }}</p>
        <div class="perms">
          @for (p of u.permissions; track p) { <span class="badge">{{ p }}</span> }
          @if (u.permissions.length === 0) { <span class="muted">No management permissions granted.</span> }
        </div>
      </div>
    }
  `,
  styles: [`
    .cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 1rem; margin: 1rem 0 1.5rem; }
    .tile { display: flex; flex-direction: column; gap: 0.25rem; color: var(--text); }
    .tile:hover { border-color: var(--primary); text-decoration: none; }
    .perms { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-top: 0.5rem; }
  `],
})
export class Dashboard {
  readonly auth = inject(AuthStore);
  readonly sections = computed(() =>
    ADMIN_NAV.filter((i) => i.path !== '/dashboard' && (!i.permission || this.auth.hasPermission(i.permission))));
}
