import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { OrdersAdminApi } from '@cs/api/orders-admin.api';
import { AuthStore } from '@cs/auth/auth.store';
import { ADMIN_NAV } from '../admin-permissions';

interface Kpi { status: string; label: string; hint: string; tone: string; }

const KPIS: Kpi[] = [
  { status: 'Paid', label: 'To process', hint: 'Paid, waiting for you', tone: 'accent' },
  { status: 'Confirmed', label: 'To process (on delivery)', hint: 'Confirmed, paid on delivery or in store', tone: 'accent' },
  { status: 'Processing', label: 'Processing', hint: 'Being prepared', tone: 'warn' },
  { status: 'Shipped', label: 'Shipped', hint: 'On their way', tone: 'info' },
  { status: 'Delivered', label: 'Delivered', hint: 'Completed', tone: 'ok' },
];

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Welcome{{ auth.user() ? ', ' + auth.user()!.firstName : '' }}</h1>
        <p class="muted sub">Here is what needs attention. You see only the sections your role permits.</p>
      </div>
    </div>

    @if (canSeeOrders()) {
      <div class="kpis">
        @for (k of kpis; track k.status) {
          <a class="card kpi" [class.accent]="k.tone === 'accent'" routerLink="/orders">
            <span class="label">{{ k.label }}</span>
            <strong class="value">{{ counts()?.[k.status] ?? '–' }}</strong>
            <span class="hint">{{ k.hint }}</span>
          </a>
        }
      </div>
    }

    <h2 class="section">Manage</h2>
    <div class="tiles">
      @for (item of sections(); track item.path) {
        <a class="card tile" [routerLink]="item.path">
          <strong>{{ item.label }}</strong>
          <span class="muted">Open {{ item.label.toLowerCase() }}</span>
          <span class="go" aria-hidden="true">→</span>
        </a>
      }
    </div>

    @if (auth.user(); as u) {
      <div class="card access">
        <h2>Your access</h2>
        <p class="muted">Roles: {{ u.roles.join(', ') || '—' }}</p>
        <div class="chips">
          @for (p of u.permissions; track p) { <span class="badge">{{ p }}</span> }
          @if (u.permissions.length === 0) { <span class="muted">No management permissions granted.</span> }
        </div>
      </div>
    }
  `,
  styles: [`
    .sub { margin: var(--space-1) 0 0; }
    .kpis { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: var(--space-4); margin-bottom: var(--space-6); }
    .kpi { display: flex; flex-direction: column; gap: var(--space-1); color: var(--text); transition: border-color 0.15s, transform 0.15s; }
    .kpi:hover { text-decoration: none; border-color: var(--border-strong); transform: translateY(-2px); }
    .kpi .label { font-size: 0.78rem; font-weight: 650; text-transform: uppercase; letter-spacing: 0.06em; color: var(--muted); }
    .kpi .value { font-size: 2.4rem; letter-spacing: -0.03em; line-height: 1.1; }
    .kpi .hint { font-size: 0.85rem; color: var(--muted); }
    .kpi.accent { background: var(--primary); border-color: var(--primary); color: #fff; }
    .kpi.accent .label, .kpi.accent .hint { color: rgba(255, 255, 255, 0.78); }
    .kpi.accent .value { color: var(--accent); }
    .section { margin-bottom: var(--space-3); }
    .tiles { display: grid; grid-template-columns: repeat(auto-fill, minmax(210px, 1fr)); gap: var(--space-4); margin-bottom: var(--space-6); }
    .tile { position: relative; display: flex; flex-direction: column; gap: var(--space-1); color: var(--text); }
    .tile:hover { text-decoration: none; border-color: var(--text); }
    .tile .go { position: absolute; right: var(--space-5); top: var(--space-5); color: var(--muted); }
    .tile:hover .go { color: var(--text); }
    .access .chips { margin-top: var(--space-2); }
  `],
})
export class Dashboard implements OnInit {
  readonly auth = inject(AuthStore);
  private readonly orders = inject(OrdersAdminApi);

  readonly kpis = KPIS;
  readonly counts = signal<Record<string, number> | null>(null);
  readonly canSeeOrders = computed(() => this.auth.hasPermission('orders.read_all'));
  readonly sections = computed(() =>
    ADMIN_NAV.filter((i) => i.path !== '/dashboard' && (!i.permission || this.auth.hasPermission(i.permission))));

  ngOnInit(): void {
    if (!this.canSeeOrders()) return;
    // Count-only queries (page size 1) so the dashboard never downloads orders just to show a number.
    forkJoin(KPIS.map((k) => this.orders.search(k.status, 1, 1))).subscribe({
      next: (pages) => this.counts.set(Object.fromEntries(KPIS.map((k, i) => [k.status, pages[i].totalCount]))),
      error: () => this.counts.set(null),
    });
  }
}
