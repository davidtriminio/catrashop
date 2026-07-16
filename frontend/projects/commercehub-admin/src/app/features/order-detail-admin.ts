import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrdersAdminApi } from '@ch/api/orders-admin.api';
import { MoneyPipe } from '@ch/util/money.pipe';
import { OrderDetail } from '@ch/api/models';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-order-detail-admin',
  imports: [DatePipe, RouterLink, MoneyPipe],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    @if (order(); as o) {
      <a routerLink="/orders" class="muted">← All orders</a>
      <div class="head"><h1>Order {{ o.orderNumber }}</h1><span class="badge">{{ o.status }}</span></div>
      <p class="muted">Customer {{ o.customerId }} · placed {{ o.createdAtUtc | date: 'medium' }}</p>

      <div class="row actions">
        <button class="btn" [disabled]="o.status !== 'Paid' || busy()" (click)="act('process', o)">Start processing</button>
        <button class="btn" [disabled]="o.status !== 'Processing' || busy()" (click)="act('ship', o)">Mark shipped</button>
        <button class="btn" [disabled]="o.status !== 'Shipped' || busy()" (click)="act('deliver', o)">Mark delivered</button>
        <button class="btn danger" [disabled]="busy()" (click)="act('cancel', o)">Cancel</button>
      </div>

      <div class="grids">
        <div class="card">
          <h2>Items</h2>
          <table><tbody>
            @for (i of o.items; track i.id) {
              <tr><td>{{ i.productName }} <span class="muted">{{ i.sku }} × {{ i.quantity }}</span></td><td class="right">{{ i.lineTotal | money: o.currency }}</td></tr>
            }
          </tbody></table>
          <div class="totals">
            <div class="line"><span>Subtotal</span><span>{{ o.subtotal | money: o.currency }}</span></div>
            @if (o.discountTotal > 0) { <div class="line"><span>Discount</span><span>−{{ o.discountTotal | money: o.currency }}</span></div> }
            <div class="line"><span>Tax</span><span>{{ o.taxTotal | money: o.currency }}</span></div>
            <div class="line total"><span>Total</span><strong>{{ o.grandTotal | money: o.currency }}</strong></div>
          </div>
        </div>
        <div class="card">
          <h2>Ship to</h2>
          <p>{{ o.shippingAddress.recipientName }}<br />{{ o.shippingAddress.line1 }}<br />{{ o.shippingAddress.city }}, {{ o.shippingAddress.postalCode }}<br />{{ o.shippingAddress.country }}</p>
          <h2>History</h2>
          <ul class="timeline">@for (h of o.history; track h.occurredAtUtc) { <li><strong>{{ h.status }}</strong> <span class="muted">{{ h.occurredAtUtc | date: 'short' }}</span></li> }</ul>
        </div>
      </div>
    }
  `,
  styles: [`
    .head { display: flex; align-items: center; gap: 0.8rem; }
    .actions { margin: 1rem 0; flex-wrap: wrap; }
    .grids { display: grid; grid-template-columns: 1.5fr 1fr; gap: 1.5rem; align-items: start; }
    .line { display: flex; justify-content: space-between; padding: 0.3rem 0; }
    .line.total { border-top: 1px solid var(--border); margin-top: 0.4rem; padding-top: 0.6rem; }
    .timeline { list-style: none; padding: 0; display: flex; flex-direction: column; gap: 0.3rem; }
    @media (max-width: 760px) { .grids { grid-template-columns: 1fr; } }
  `],
})
export class OrderDetailAdmin implements OnInit {
  private readonly api = inject(OrdersAdminApi);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<OrderDetail | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  private id = computed(() => this.route.snapshot.paramMap.get('id')!);

  ngOnInit(): void { this.load(); }
  private load(): void {
    this.api.get(this.id()).subscribe({ next: (o) => this.order.set(o), error: (e) => this.error.set(problemMessage(e, 'Order not found.')) });
  }

  act(action: 'process' | 'ship' | 'deliver' | 'cancel', o: OrderDetail): void {
    this.busy.set(true); this.error.set(null); this.message.set(null);
    const op = action === 'process' ? this.api.process(o.id)
      : action === 'ship' ? this.api.ship(o.id)
      : action === 'deliver' ? this.api.deliver(o.id)
      : this.api.cancel(o.id, 'Cancelled by staff');
    op.subscribe({
      next: () => { this.message.set('Order updated.'); this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(problemMessage(e)); this.busy.set(false); },
    });
  }
}
