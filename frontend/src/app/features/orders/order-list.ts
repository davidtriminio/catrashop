import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrdersApi } from '@ch/api/orders.api';
import { OrderSummary, Paged } from '@ch/api/models';
import { StatusBadge } from '@ch/ui/status-badge';
import { I18n } from '@ch/i18n/i18n';
import { LocalDatePipe } from '@ch/util/local-date.pipe';
import { MoneyPipe } from '@ch/util/money.pipe';
import { PluralPipe } from '@ch/util/plural.pipe';
import { buildPages } from '@ch/util/pagination';

@Component({
  selector: 'app-order-list',
  imports: [RouterLink, LocalDatePipe, MoneyPipe, PluralPipe, StatusBadge, TranslocoPipe],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ 'orders.title' | transloco }}</h1>
        @if (result(); as r) { <p class="muted count">{{ 'orders.count' | plural: r.totalCount }}</p> }
      </div>
    </div>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (loading() && !result()) {
      <div class="card flush">
        @for (n of [1, 2, 3, 4]; track n) { <div class="list-row"><div class="skeleton" style="height:2.4rem;width:100%"></div></div> }
      </div>
    } @else if (result(); as r) {
      @if (r.items.length === 0) {
        <div class="card empty">
          <h2>{{ 'orders.emptyTitle' | transloco }}</h2>
          <p>{{ 'orders.emptyText' | transloco }}</p>
          <a routerLink="/products" class="btn">{{ 'orders.startShopping' | transloco }}</a>
        </div>
      } @else {
        <div class="card flush" [class.dim]="loading()">
          @for (o of r.items; track o.id) {
            <a class="list-row order" [routerLink]="['/orders', o.id]">
              <div class="who">
                <strong>{{ o.orderNumber }}</strong>
                <span class="muted">{{ o.createdAtUtc | localDate: 'date' }} · {{ 'cart.items' | plural: o.itemCount }}</span>
              </div>
              <ch-status-badge [status]="o.status" />
              <strong class="total">{{ o.grandTotal | money: o.currency }}</strong>
              <span class="chev" aria-hidden="true">›</span>
            </a>
          }
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
    }
  `,
  styles: [`
    .count { margin: var(--space-1) 0 0; }
    .dim { opacity: 0.6; }
    .order { display: grid; grid-template-columns: minmax(0, 1fr) auto auto 1rem; align-items: center; gap: var(--space-4); color: var(--text); }
    .order:hover { background: var(--surface-2); text-decoration: none; }
    .who { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
    .who .muted { font-size: 0.82rem; }
    .total { min-width: 6rem; text-align: right; }
    .chev { color: var(--muted); font-size: 1.3rem; line-height: 1; }
    .pager { display: flex; align-items: center; justify-content: center; gap: var(--space-2); margin: var(--space-6) 0 var(--space-2); flex-wrap: wrap; }
    .page { min-width: 2.2rem; height: 2.2rem; border-radius: var(--radius-pill); border: 1px solid transparent; background: none; font: inherit; font-weight: 600; cursor: pointer; }
    .page:hover { border-color: var(--border-strong); }
    .page.current { background: var(--primary); color: var(--primary-contrast); }
    .empty .btn { margin-top: var(--space-2); }
    .empty h2 { color: var(--text); }
    @media (max-width: 560px) {
      .order { grid-template-columns: minmax(0, 1fr) auto 1rem; row-gap: var(--space-2); }
      .order ch-status-badge { grid-column: 1; grid-row: 2; justify-self: start; }
      .order .total { grid-column: 2; grid-row: 1; min-width: 0; }
      .order .chev { grid-column: 3; grid-row: 1 / span 2; }
    }
  `],
})
export class OrderList implements OnInit {
  private readonly api = inject(OrdersApi);
  private readonly i18n = inject(I18n);
  private page = 1;

  readonly result = signal<Paged<OrderSummary> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pages = computed(() => buildPages(this.result()?.page ?? 1, this.result()?.totalPages ?? 0));

  ngOnInit(): void { this.load(); }

  go(page: number | '…'): void {
    if (page === '…') return;
    this.page = page;
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listMine(this.page, 10).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (err) => { this.error.set(this.i18n.problem(err)); this.loading.set(false); },
    });
  }
}
