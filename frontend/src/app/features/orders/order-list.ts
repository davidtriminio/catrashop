import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrdersApi } from '@ch/api/orders.api';
import { OrderSummary, Paged } from '@ch/api/models';
import { MoneyPipe } from '@ch/util/money.pipe';
import { OrderStatusBadge } from './order-status-badge';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-order-list',
  imports: [RouterLink, DatePipe, MoneyPipe, OrderStatusBadge],
  template: `
    <h1>Your orders</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (loading()) { <p class="muted">Loading…</p> }

    @if (result(); as r) {
      @if (r.items.length === 0) {
        <div class="card"><p class="muted">You have no orders yet.</p><a routerLink="/products" class="btn">Start shopping</a></div>
      } @else {
        <div class="card">
          <table>
            <thead><tr><th>Order</th><th>Date</th><th>Status</th><th>Items</th><th class="right">Total</th><th></th></tr></thead>
            <tbody>
              @for (o of r.items; track o.id) {
                <tr>
                  <td><strong>{{ o.orderNumber }}</strong></td>
                  <td class="muted">{{ o.createdAtUtc | date: 'medium' }}</td>
                  <td><app-order-status-badge [status]="o.status" /></td>
                  <td>{{ o.itemCount }}</td>
                  <td class="right">{{ o.grandTotal | money: o.currency }}</td>
                  <td class="right"><a class="btn secondary sm" [routerLink]="['/orders', o.id]">View</a></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <div class="pager">
          <button class="btn secondary sm" [disabled]="!r.hasPrevious" (click)="go(r.page - 1)">Previous</button>
          <span class="muted">Page {{ r.page }} of {{ r.totalPages || 1 }}</span>
          <button class="btn secondary sm" [disabled]="!r.hasNext" (click)="go(r.page + 1)">Next</button>
        </div>
      }
    }
  `,
  styles: [`.pager { display: flex; align-items: center; justify-content: center; gap: 1rem; margin: 1.5rem 0; }`],
})
export class OrderList implements OnInit {
  private readonly api = inject(OrdersApi);
  private page = 1;

  readonly result = signal<Paged<OrderSummary> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.load(); }
  go(page: number): void { this.page = page; this.load(); }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listMine(this.page, 10).subscribe({
      next: (r) => { this.result.set(r); this.loading.set(false); },
      error: (err) => { this.error.set(problemMessage(err)); this.loading.set(false); },
    });
  }
}
