import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n, LOCALES, Lang } from '@cs/i18n/i18n';
import { LocalDatePipe } from '@cs/util/local-date.pipe';
import { forkJoin } from 'rxjs';
import { OrdersAdminApi } from '@cs/api/orders-admin.api';
import { OrderSummary } from '@cs/api/models';
import { AuthStore } from '@cs/auth/auth.store';
import { StatusBadge } from '@cs/ui/status-badge';
import { MoneyPipe } from '@cs/util/money.pipe';
import { ADMIN_NAV } from '../admin-permissions';
import { Icon, IconName } from '../ui/icon';
import { AccessCard } from './access-card';

interface Kpi { status: string; label: string; hint: string; icon: IconName; featured?: boolean; color: string; }

const KPIS: Kpi[] = [
  { status: 'Paid', label: 'admin.dashboard.kpi.toProcess', hint: 'admin.dashboard.kpi.toProcessHint', icon: 'bag', featured: true, color: 'var(--chart-1)' },
  { status: 'Confirmed', label: 'admin.dashboard.kpi.toProcessCod', hint: 'admin.dashboard.kpi.toProcessCodHint', icon: 'clock', color: 'var(--chart-2)' },
  { status: 'Processing', label: 'admin.dashboard.kpi.processing', hint: 'admin.dashboard.kpi.processingHint', icon: 'package', color: 'var(--chart-3)' },
  { status: 'Shipped', label: 'admin.dashboard.kpi.shipped', hint: 'admin.dashboard.kpi.shippedHint', icon: 'truck', color: 'var(--chart-4)' },
  { status: 'Delivered', label: 'admin.dashboard.kpi.delivered', hint: 'admin.dashboard.kpi.deliveredHint', icon: 'check', color: 'var(--chart-5)' },
];

/** Cancelled or failed orders do not count as order value. */
const NOT_REVENUE = new Set(['Cancelled', 'PaymentFailed', 'Failed']);
const DAYS = 14;
const W = 640, H = 220, PAD_X = 12, PAD_TOP = 16, PAD_BOTTOM = 28;
const R = 64, CIRC = 2 * Math.PI * R;

const dayKey = (d: Date) => `${d.getFullYear()}-${d.getMonth()}-${d.getDate()}`;

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, LocalDatePipe, MoneyPipe, StatusBadge, Icon, TranslocoPipe, AccessCard],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ 'admin.dashboard.welcome' | transloco }}{{ auth.user() ? ', ' + auth.user()!.firstName : '' }}</h1>
        <p class="muted sub">{{ 'admin.dashboard.sub' | transloco }}</p>
      </div>
      <span class="today muted">{{ today | localDate: 'date' }}</span>
    </div>

    @if (canSeeOrders()) {
      <div class="kpis">
        @for (k of kpis; track k.status) {
          <a class="card kpi" [class.featured]="k.featured" routerLink="/orders">
            <span class="ico" [style.--tone]="k.color"><cs-icon [name]="k.icon" [size]="18" /></span>
            <span class="label">{{ k.label | transloco }}</span>
            <strong class="value">{{ counts()?.[k.status] ?? '–' }}</strong>
            <span class="hint">{{ k.hint | transloco }}</span>
          </a>
        }
      </div>

      <div class="charts">
        <section class="card">
          <header class="card-head">
            <div>
              <h2>{{ 'admin.dashboard.orderValue' | transloco }}</h2>
              <p class="muted small">{{ 'admin.dashboard.valueSub' | transloco: { days: days, count: recent().length } }}</p>
            </div>
            <strong class="total">{{ series().total | money: currency() }}</strong>
          </header>
          @if (loadedRecent()) {
            <svg class="area" [attr.viewBox]="'0 0 ' + W + ' ' + H" role="img"
                 [attr.aria-label]="'admin.dashboard.valueAria' | transloco: { days: days }" preserveAspectRatio="none">
              <defs>
                <linearGradient id="areaFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" style="stop-color: var(--chart-1); stop-opacity: 0.32" />
                  <stop offset="100%" style="stop-color: var(--chart-1); stop-opacity: 0" />
                </linearGradient>
              </defs>
              @for (g of grid; track g) { <line [attr.x1]="PAD_X" [attr.x2]="W - PAD_X" [attr.y1]="g" [attr.y2]="g" class="grid-line" /> }
              <path [attr.d]="series().area" fill="url(#areaFill)" />
              <path [attr.d]="series().line" class="line" fill="none" />
              @for (p of series().points; track $index) {
                <circle [attr.cx]="p.x" [attr.cy]="p.y" r="3.5" class="dot"><title>{{ p.label }}: {{ p.value | money: currency() }}</title></circle>
              }
            </svg>
            <div class="x-axis" aria-hidden="true">
              <span>{{ series().from }}</span><span>{{ series().to }}</span>
            </div>
          } @else {
            <div class="skeleton chart-skeleton"></div>
          }
        </section>

        <section class="card">
          <header class="card-head"><div><h2>{{ 'admin.dashboard.byStatus' | transloco }}</h2><p class="muted small">{{ 'admin.dashboard.byStatusSub' | transloco }}</p></div></header>
          <div class="donut-wrap">
            <svg viewBox="0 0 180 180" class="donut" role="img" [aria-label]="'admin.dashboard.byStatus' | transloco">
              <circle cx="90" cy="90" [attr.r]="R" class="ring-bg" />
              @for (s of segments(); track s.status) {
                <circle cx="90" cy="90" [attr.r]="R" class="seg" [style.stroke]="s.color"
                        [attr.stroke-dasharray]="s.len + ' ' + (CIRC - s.len)" [attr.stroke-dashoffset]="-s.offset">
                  <title>{{ s.label | transloco }}: {{ s.count }}</title>
                </circle>
              }
              <text x="90" y="88" text-anchor="middle" class="donut-n">{{ totalCount() }}</text>
              <text x="90" y="108" text-anchor="middle" class="donut-l">{{ 'admin.dashboard.orders' | transloco }}</text>
            </svg>
            <ul class="legend">
              @for (k of kpis; track k.status) {
                <li><i [style.background]="k.color"></i><span>{{ k.label | transloco }}</span><b>{{ counts()?.[k.status] ?? '–' }}</b></li>
              }
            </ul>
          </div>
        </section>
      </div>

      <section class="card flush recent">
        <header class="card-head pad">
          <div><h2>{{ 'admin.dashboard.recent' | transloco }}</h2></div>
          <a class="btn secondary sm" routerLink="/orders">{{ 'admin.dashboard.viewAll' | transloco }} <cs-icon name="arrow" [size]="14" /></a>
        </header>
        <div class="scroll">
          <table>
            <thead><tr><th>{{ 'admin.c.order' | transloco }}</th><th>{{ 'admin.c.date' | transloco }}</th><th>{{ 'admin.c.status' | transloco }}</th><th>{{ 'admin.c.items' | transloco }}</th><th class="right">{{ 'admin.c.total' | transloco }}</th></tr></thead>
            <tbody>
              @for (o of latest(); track o.id) {
                <tr>
                  <td><a [routerLink]="['/orders', o.id]">{{ o.orderNumber }}</a></td>
                  <td class="muted">{{ o.createdAtUtc | localDate: 'datetime' }}</td>
                  <td><cs-status-badge [status]="o.status" /></td>
                  <td>{{ o.itemCount }}</td>
                  <td class="right"><strong>{{ o.grandTotal | money: o.currency }}</strong></td>
                </tr>
              } @empty { <tr><td colspan="5" class="empty">{{ (loadedRecent() ? 'admin.dashboard.noOrders' : 'common.loading') | transloco }}</td></tr> }
            </tbody>
          </table>
        </div>
      </section>
    }

    <h2 class="section">{{ 'admin.dashboard.manage' | transloco }}</h2>
    <div class="tiles">
      @for (item of sections(); track item.path) {
        <a class="card tile" [routerLink]="item.path">
          <span class="ico plain"><cs-icon [name]="item.icon" [size]="18" /></span>
          <strong>{{ item.labelKey | transloco }}</strong>
          <cs-icon class="go" name="arrow" [size]="16" />
        </a>
      }
    </div>

    <app-access-card />
  `,
  styles: [`
    .sub { margin: var(--space-1) 0 0; }
    .today { font-size: 0.85rem; white-space: nowrap; }
    .small { font-size: 0.8rem; margin: 0; }
    .section { margin: var(--space-6) 0 var(--space-3); }

    .ico { display: grid; place-items: center; width: 36px; height: 36px; border-radius: 10px; margin-bottom: var(--space-3);
           color: var(--tone, var(--primary)); background: color-mix(in srgb, var(--tone, var(--primary)) 16%, transparent); }
    .ico.plain { background: var(--accent); color: var(--accent-ink); margin: 0; }

    .kpis { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: var(--space-4); margin-bottom: var(--space-4); }
    .kpi { display: flex; flex-direction: column; color: var(--text); transition: border-color 0.15s, transform 0.15s, box-shadow 0.15s; }
    .kpi:hover { text-decoration: none; border-color: var(--primary); transform: translateY(-2px); box-shadow: var(--shadow-raised); }
    .kpi .label { font-size: 0.8rem; font-weight: 600; color: var(--muted); }
    .kpi .value { font-size: 2.1rem; letter-spacing: -0.03em; line-height: 1.15; margin: var(--space-1) 0; }
    .kpi .hint { font-size: 0.8rem; color: var(--muted); }
    .kpi.featured { background: linear-gradient(145deg, #0a2a7a, #003399 60%, #1a4bd1); border-color: transparent; color: #fff; }
    .kpi.featured .label, .kpi.featured .hint { color: rgba(255, 255, 255, 0.78); }
    .kpi.featured .ico { background: rgba(255, 255, 255, 0.16); color: #fff; }

    .charts { display: grid; grid-template-columns: minmax(0, 1.8fr) minmax(0, 1fr); gap: var(--space-4); margin-bottom: var(--space-4); }
    .charts > .card, .recent { margin-top: 0; }
    .card-head { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--space-4); margin-bottom: var(--space-4); }
    .card-head h2 { margin: 0; }
    .card-head.pad { padding: var(--space-4) var(--space-5); margin: 0; border-bottom: 1px solid var(--border); align-items: center; }
    .total { font-size: 1.15rem; letter-spacing: -0.02em; white-space: nowrap; }

    .area { width: 100%; height: 220px; display: block; overflow: visible; }
    .grid-line { stroke: var(--chart-grid); stroke-width: 1; vector-effect: non-scaling-stroke; }
    .line { stroke: var(--chart-1); stroke-width: 2.5; stroke-linejoin: round; stroke-linecap: round; vector-effect: non-scaling-stroke; }
    .dot { fill: var(--surface); stroke: var(--chart-1); stroke-width: 2; vector-effect: non-scaling-stroke; }
    .x-axis { display: flex; justify-content: space-between; font-size: 0.75rem; color: var(--muted); margin-top: var(--space-2); }
    .chart-skeleton { height: 220px; }

    .donut-wrap { display: flex; align-items: center; gap: var(--space-5); flex-wrap: wrap; justify-content: center; }
    .donut { width: 170px; height: 170px; flex-shrink: 0; }
    .ring-bg { fill: none; stroke: var(--chart-grid); stroke-width: 20; }
    .seg { fill: none; stroke-width: 20; transform: rotate(-90deg); transform-origin: 90px 90px; }
    .donut-n { fill: var(--text); font-size: 28px; font-weight: 700; letter-spacing: -0.03em; }
    .donut-l { fill: var(--muted); font-size: 11px; }
    .legend { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--space-2); flex: 1; min-width: 150px; }
    .legend li { display: flex; align-items: center; gap: var(--space-2); font-size: 0.85rem; }
    .legend i { width: 10px; height: 10px; border-radius: 3px; flex-shrink: 0; }
    .legend span { flex: 1; color: var(--muted); }

    .scroll { overflow-x: auto; }
    .tiles { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: var(--space-3); margin-bottom: var(--space-5); }
    .tile { display: flex; align-items: center; gap: var(--space-3); padding: var(--space-4); color: var(--text); }
    .tile:hover { text-decoration: none; border-color: var(--primary); }
    .tile strong { flex: 1; font-weight: 600; }
    .tile .go { color: var(--muted); transition: transform 0.15s; }
    .tile:hover .go { transform: translateX(3px); color: var(--primary); }
    @media (max-width: 1000px) { .charts { grid-template-columns: 1fr; } }
    @media (max-width: 600px) {
      .today { display: none; }
      .kpis { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: var(--space-3); }
      .kpi:first-child { grid-column: 1 / -1; }
    }
    @media (prefers-reduced-motion: reduce) { .kpi, .tile .go { transition: none; } }
  `],
})
export class Dashboard implements OnInit {
  readonly auth = inject(AuthStore);
  private readonly orders = inject(OrdersAdminApi);
  private readonly i18n = inject(I18n);

  readonly kpis = KPIS;
  readonly days = DAYS;
  readonly today = new Date();
  readonly W = W; readonly H = H; readonly PAD_X = PAD_X; readonly R = R; readonly CIRC = CIRC;
  readonly grid = [0, 1, 2, 3].map((i) => PAD_TOP + (i * (H - PAD_TOP - PAD_BOTTOM)) / 3);

  readonly counts = signal<Record<string, number> | null>(null);
  readonly recent = signal<OrderSummary[]>([]);
  readonly loadedRecent = signal(false);
  readonly canSeeOrders = computed(() => this.auth.hasPermission('orders.read_all'));
  readonly sections = computed(() =>
    ADMIN_NAV.filter((i) => i.path !== '/dashboard' && (!i.permission || this.auth.hasPermission(i.permission))));

  readonly latest = computed(() => this.recent().slice(0, 6));
  readonly currency = computed(() => this.recent()[0]?.currency);
  readonly totalCount = computed(() => KPIS.reduce((n, k) => n + (this.counts()?.[k.status] ?? 0), 0));

  readonly segments = computed(() => {
    const total = this.totalCount();
    let offset = 0;
    return KPIS.map((k) => {
      const count = this.counts()?.[k.status] ?? 0;
      const share = total ? (count / total) * CIRC : 0;
      const len = Math.max(0, share - (count && total > count ? 2 : 0)); // small gap between segments
      const seg = { status: k.status, label: k.label, color: k.color, count, len, offset };
      offset += share;
      return seg;
    }).filter((s) => s.count > 0);
  });

  /** Daily order value over the last DAYS days, drawn as an area chart. */
  readonly series = computed(() => {
    const today = new Date(); today.setHours(0, 0, 0, 0);
    const days = Array.from({ length: DAYS }, (_, i) => { const d = new Date(today); d.setDate(d.getDate() - (DAYS - 1 - i)); return d; });
    const sums = new Map(days.map((d) => [dayKey(d), 0]));
    for (const o of this.recent()) {
      if (NOT_REVENUE.has(o.status)) continue;
      const k = dayKey(new Date(o.createdAtUtc));
      if (sums.has(k)) sums.set(k, sums.get(k)! + o.grandTotal);
    }
    const values = days.map((d) => sums.get(dayKey(d))!);
    const max = Math.max(...values, 1);
    const innerH = H - PAD_TOP - PAD_BOTTOM;
    const points = values.map((value, i) => ({
      x: PAD_X + (i * (W - 2 * PAD_X)) / (DAYS - 1),
      y: PAD_TOP + innerH - (value / max) * innerH,
      value,
      label: days[i].toLocaleDateString(LOCALES[this.i18n.lang() as Lang], { month: 'short', day: 'numeric' }),
    }));
    const line = points.map((p, i) => `${i ? 'L' : 'M'}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join('');
    const base = PAD_TOP + innerH;
    const area = `${line}L${points[points.length - 1].x.toFixed(1)} ${base}L${points[0].x.toFixed(1)} ${base}Z`;
    return { points, line, area, total: values.reduce((a, b) => a + b, 0), from: points[0].label, to: points[points.length - 1].label };
  });

  ngOnInit(): void {
    if (!this.canSeeOrders()) return;
    // Count-only queries (page size 1) so the dashboard never downloads orders just to show a number.
    forkJoin(KPIS.map((k) => this.orders.search(k.status, 1, 1))).subscribe({
      next: (pages) => this.counts.set(Object.fromEntries(KPIS.map((k, i) => [k.status, pages[i].totalCount]))),
      error: () => this.counts.set(null),
    });
    this.orders.search(undefined, 1, 100).subscribe({
      next: (page) => { this.recent.set(page.items); this.loadedRecent.set(true); },
      error: () => this.loadedRecent.set(true),
    });
  }
}
