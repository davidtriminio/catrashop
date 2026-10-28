import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrdersAdminApi } from '@ch/api/orders-admin.api';
import { OrderSummary, Paged } from '@ch/api/models';
import { StatusBadge } from '@ch/ui/status-badge';
import { MoneyPipe } from '@ch/util/money.pipe';
import { buildPages } from '@ch/util/pagination';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-orders-admin',
  imports: [RouterLink, DatePipe, MoneyPipe, StatusBadge],
  template: `
    <div class="page-head">
      <div>
        <h1>Orders</h1>
        @if (result(); as r) { <p class="muted count">{{ r.totalCount }} {{ r.totalCount === 1 ? 'order' : 'orders' }}@if (status) { with this status }</p> }
      </div>
    </div>

    <div class="chips filters" role="group" aria-label="Filter by status">
      <button class="chip" type="button" [class.active]="!status" (click)="setStatus('')">All</button>
      @for (s of statuses; track s) {
        <button class="chip" type="button" [class.active]="status === s" (click)="setStatus(s)">{{ label(s) }}</button>
      }
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (result(); as r) {
      <div class="card flush" [class.dim]="loading()">
        <table>
          <thead><tr><th>Order</th><th>Date</th><th>Status</th><th>Items</th><th class="right">Total</th><th></th></tr></thead>
          <tbody>
            @for (o of r.items; track o.id) {
              <tr>
                <td><a class="num" [routerLink]="['/orders', o.id]">{{ o.orderNumber }}</a></td>
                <td class="muted">{{ o.createdAtUtc | date: 'MMM d, y, h:mm a' }}</td>
                <td><ch-status-badge [status]="o.status" /></td>
                <td>{{ o.itemCount }}</td>
                <td class="right"><strong>{{ o.grandTotal | money: o.currency }}</strong></td>
                <td class="right"><a class="btn secondary sm" [routerLink]="['/orders', o.id]">Open</a></td>
              </tr>
            } @empty { <tr><td colspan="6" class="empty">No orders{{ status ? ' with this status' : '' }}.</td></tr> }
          </tbody>
        </table>
      </div>

      @if (r.totalPages > 1) {
        <nav class="pager" aria-label="Pagination">
          <button class="btn secondary sm" type="button" [disabled]="!r.hasPrevious" (click)="go(r.page - 1)">Previous</button>
          @for (item of pages(); track $index) {
            @if (item === '…') { <span class="muted">…</span> }
            @else { <button class="page" type="button" [class.current]="item === r.page" (click)="go(item)">{{ item }}</button> }
          }
          <button class="btn secondary sm" type="button" [disabled]="!r.hasNext" (click)="go(r.page + 1)">Next</button>
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
  private page = 1;
  status = '';
  readonly statuses = ['Paid', 'Processing', 'Shipped', 'Delivered', 'Pending', 'PaymentPending', 'PaymentFailed', 'Cancelled', 'RefundRequested', 'Refunded'];

  readonly result = signal<Paged<OrderSummary> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pages = computed(() => buildPages(this.result()?.page ?? 1, this.result()?.totalPages ?? 0));

  ngOnInit(): void { this.load(); }

  label(s: string): string {
    const spaced = s.replace(/([a-z])([A-Z])/g, '$1 $2');
    return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
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
      error: (e) => { this.error.set(problemMessage(e)); this.loading.set(false); },
    });
  }
}
