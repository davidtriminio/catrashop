import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OrdersAdminApi } from '@ch/api/orders-admin.api';
import { MoneyPipe } from '@ch/util/money.pipe';
import { OrderSummary, Paged } from '@ch/api/models';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-orders-admin',
  imports: [FormsModule, RouterLink, DatePipe, MoneyPipe],
  template: `
    <h1>Orders</h1>
    <div class="card bar">
      <label>Status
        <select [(ngModel)]="status" (change)="load()">
          <option value="">All</option>
          @for (s of statuses; track s) { <option [value]="s">{{ s }}</option> }
        </select>
      </label>
    </div>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (result(); as r) {
      <div class="card">
        <table>
          <thead><tr><th>Order</th><th>Date</th><th>Status</th><th>Items</th><th class="right">Total</th><th></th></tr></thead>
          <tbody>
            @for (o of r.items; track o.id) {
              <tr>
                <td><strong>{{ o.orderNumber }}</strong></td>
                <td class="muted">{{ o.createdAtUtc | date: 'short' }}</td>
                <td><span class="badge">{{ o.status }}</span></td>
                <td>{{ o.itemCount }}</td>
                <td class="right">{{ o.grandTotal | money: o.currency }}</td>
                <td class="right"><a class="btn secondary sm" [routerLink]="['/orders', o.id]">Open</a></td>
              </tr>
            } @empty { <tr><td colspan="6" class="muted">No orders.</td></tr> }
          </tbody>
        </table>
      </div>
      <div class="pager">
        <button class="btn secondary sm" [disabled]="!r.hasPrevious" (click)="go(r.page - 1)">Previous</button>
        <span class="muted">Page {{ r.page }} / {{ r.totalPages || 1 }}</span>
        <button class="btn secondary sm" [disabled]="!r.hasNext" (click)="go(r.page + 1)">Next</button>
      </div>
    }
  `,
  styles: [`.bar label { font-size: 0.85rem; font-weight: 600; } .bar select { margin-left: 0.5rem; width: auto; }
    .pager { display: flex; gap: 1rem; align-items: center; justify-content: center; margin: 1rem 0; }`],
})
export class OrdersAdmin implements OnInit {
  private readonly api = inject(OrdersAdminApi);
  private page = 1;
  status = '';
  readonly statuses = ['Pending', 'PaymentPending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'PaymentFailed', 'Cancelled', 'RefundRequested', 'Refunded'];

  readonly result = signal<Paged<OrderSummary> | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void { this.load(); }
  go(p: number): void { this.page = p; this.load(); }
  load(): void {
    this.api.search(this.status || undefined, this.page, 20).subscribe({
      next: (r) => this.result.set(r), error: (e) => this.error.set(problemMessage(e)),
    });
  }
}
