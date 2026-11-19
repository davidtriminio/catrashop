import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { LocalDatePipe } from '@cs/util/local-date.pipe';
import { OrdersAdminApi } from '@cs/api/orders-admin.api';
import { OrderSummary, Paged } from '@cs/api/models';
import { StatusBadge } from '@cs/ui/status-badge';
import { MoneyPipe } from '@cs/util/money.pipe';
import { buildPages } from '@cs/util/pagination';

@Component({
  selector: 'app-orders-admin',
  imports: [RouterLink, LocalDatePipe, MoneyPipe, StatusBadge, TranslocoPipe],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ 'admin.nav.orders' | transloco }}</h1>
        @if (result(); as r) { <p class="muted count">{{ r.totalCount }} {{ (r.totalCount === 1 ? 'admin.orders.one' : 'admin.orders.many') | transloco }}@if (status) { {{ 'admin.orders.withStatus' | transloco }} }</p> }
      </div>
    </div>

    <div class="chips filters" role="group" [aria-label]="'admin.orders.filter' | transloco">
      <button class="chip" type="button" [class.active]="!status" (click)="setStatus('')">{{ 'admin.orders.all' | transloco }}</button>
      @for (s of statuses; track s) {
        <button class="chip" type="button" [class.active]="status === s" (click)="setStatus(s)">{{ label(s) }}</button>
      }
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (result(); as r) {
      <div class="card flush" [class.dim]="loading()">
        <table>
          <thead><tr><th>{{ 'admin.c.order' | transloco }}</th><th>{{ 'admin.c.date' | transloco }}</th><th>{{ 'admin.c.status' | transloco }}</th><th>{{ 'admin.c.items' | transloco }}</th><th class="right">{{ 'admin.c.total' | transloco }}</th><th></th></tr></thead>
          <tbody>
            @for (o of r.items; track o.id) {
              <tr>
                <td><a class="num" [routerLink]="['/orders', o.id]">{{ o.orderNumber }}</a></td>
                <td class="muted">{{ o.createdAtUtc | localDate: 'datetime' }}</td>
                <td><cs-status-badge [status]="o.status" /></td>
                <td>{{ o.itemCount }}</td>
                <td class="right"><strong>{{ o.grandTotal | money: o.currency }}</strong></td>
                <td class="right"><a class="btn secondary sm" [routerLink]="['/orders', o.id]">{{ 'admin.c.open' | transloco }}</a></td>
              </tr>
            } @empty { <tr><td colspan="6" class="empty">{{ (status ? 'admin.orders.emptyStatus' : 'admin.orders.empty') | transloco }}</td></tr> }
          </tbody>
        </table>
      </div>

      @if (r.totalPages > 1) {
        <nav class="pager" [attr.aria-label]="'pagination.label' | transloco">
          <button class="btn secondary sm" type="button" [disabled]="!r.hasPrevious" (click)="go(r.page - 1)">{{ 'pagination.previous' | transloco }}</button>
          @for (item of pages(); track $index) {
            @if (item === '…') { <span class="muted">…</span> }
            @else { <button class="page" type="button" [class.current]="item === r.page" (click)="go(item)">{{ item }}</button> }
          }
          <button class="btn secondary sm" type="button" [disabled]="!r.hasNext" (click)="go(r.page + 1)">{{ 'pagination.next' | transloco }}</button>
        </nav>
      }
    }
  `,
  styles: [`
    .count { margin: var(--space-1) 0 0; }
    .filters { margin-bottom: var(--space-5); }
    .dim { opacity: 0.6; }
    .num { font-weight: 650; white-space: nowrap; }
    td { white-space: nowrap; }
    td .btn { visibility: hidden; }
    tr:hover td .btn, td .btn:focus-visible { visibility: visible; }
    @media (hover: none) { td .btn { visibility: visible; } }
    .card.flush { overflow-x: auto; }
    .pager { display: flex; align-items: center; justify-content: center; gap: var(--space-2); margin: var(--space-5) 0 var(--space-2); flex-wrap: wrap; }
    .page { min-width: 2.2rem; height: 2.2rem; border-radius: var(--radius-pill); border: 1px solid transparent; background: none; font: inherit; font-weight: 600; cursor: pointer; }
    .page:hover { border-color: var(--border-strong); }
    .page.current { background: var(--primary); color: var(--primary-contrast); }
  `],
})
export class OrdersAdmin implements OnInit {
  private readonly api = inject(OrdersAdminApi);
  private readonly i18n = inject(I18n);
  private page = 1;
  status = '';
  readonly statuses = ['Paid', 'Confirmed', 'Processing', 'Shipped', 'Delivered', 'Pending', 'PaymentPending', 'PaymentFailed', 'Cancelled', 'RefundRequested', 'Refunded'];

  readonly result = signal<Paged<OrderSummary> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pages = computed(() => buildPages(this.result()?.page ?? 1, this.result()?.totalPages ?? 0));

  ngOnInit(): void { this.load(); }

  label(s: string): string {
    this.i18n.lang(); // recompute when the language changes
    const spaced = s.replace(/([a-z])([A-Z])/g, '$1 $2');
    const readable = spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
    return this.i18n.tOr(`status.${s.charAt(0).toLowerCase()}${s.slice(1)}`, readable);
  }

  setStatus(s: string): void { this.status = s; this.page = 1; this.load(); }

  go(p: number | '…'): void {
    if (p === '…') return;
    this.page = p;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.search(this.status || undefined, this.page, 20).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.loading.set(false); },
    });
  }
}
