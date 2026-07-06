import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrderDetail, Tracking } from '../../core/api/models';
import { OrdersApi } from '../../core/api/orders.api';
import { PaymentsApi } from '../../core/api/payments.api';
import { ShippingApi } from '../../core/api/shipping.api';
import { MoneyPipe } from '../../shared/money.pipe';
import { OrderStatusBadge } from './order-status-badge';
import { problemMessage } from '../../shared/problem';

@Component({
  selector: 'app-order-detail',
  imports: [DatePipe, RouterLink, MoneyPipe, OrderStatusBadge],
  template: `
    @if (loading()) { <p class="muted">Loading…</p> }
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (order(); as o) {
      <a routerLink="/orders" class="muted">← All orders</a>
      <div class="head">
        <h1>Order {{ o.orderNumber }}</h1>
        <app-order-status-badge [status]="o.status" />
      </div>
      <p class="muted">Placed {{ o.createdAtUtc | date: 'medium' }}</p>

      @if (actionError()) { <div class="alert error">{{ actionError() }}</div> }
      @if (actionMessage()) { <div class="alert success">{{ actionMessage() }}</div> }

      <div class="row actions">
        @if (canPay()) { <button class="btn" [disabled]="busy()" (click)="pay(o)">Pay now</button> }
        @if (canCancel()) { <button class="btn danger" [disabled]="busy()" (click)="cancel(o)">Cancel order</button> }
      </div>

      <div class="grids">
        <div class="card">
          <h2>Items</h2>
          <table>
            <tbody>
              @for (item of o.items; track item.id) {
                <tr>
                  <td>{{ item.productName }}<br /><span class="muted">{{ item.sku }} × {{ item.quantity }}</span></td>
                  <td class="right">{{ item.lineTotal | money: o.currency }}</td>
                </tr>
              }
            </tbody>
          </table>
          <div class="totals">
            <div class="line"><span>Subtotal</span><span>{{ o.subtotal | money: o.currency }}</span></div>
            @if (o.discountTotal > 0) { <div class="line"><span>Discount</span><span>−{{ o.discountTotal | money: o.currency }}</span></div> }
            <div class="line"><span>Tax</span><span>{{ o.taxTotal | money: o.currency }}</span></div>
            <div class="line total"><span>Total</span><strong>{{ o.grandTotal | money: o.currency }}</strong></div>
          </div>
        </div>

        <div class="side">
          <div class="card">
            <h2>Ship to</h2>
            <p>
              {{ o.shippingAddress.recipientName }}<br />
              {{ o.shippingAddress.line1 }}@if (o.shippingAddress.line2) { , {{ o.shippingAddress.line2 }} }<br />
              {{ o.shippingAddress.city }}, {{ o.shippingAddress.postalCode }}<br />
              {{ o.shippingAddress.country }}
            </p>
          </div>

          @if (tracking(); as t) {
            <div class="card">
              <h2>Shipment</h2>
              <p><span class="muted">Tracking</span> <strong>{{ t.trackingNumber }}</strong> · <app-order-status-badge [status]="t.status" /></p>
              <ul class="timeline">
                @for (e of t.events; track e.occurredAtUtc) {
                  <li><strong>{{ e.status }}</strong> <span class="muted">{{ e.occurredAtUtc | date: 'short' }}</span>@if (e.note) { — {{ e.note }} }</li>
                }
              </ul>
            </div>
          }

          <div class="card">
            <h2>History</h2>
            <ul class="timeline">
              @for (h of o.history; track h.occurredAtUtc) {
                <li><strong>{{ h.status }}</strong> <span class="muted">{{ h.occurredAtUtc | date: 'short' }}</span>@if (h.note) { — {{ h.note }} }</li>
              }
            </ul>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .head { display: flex; align-items: center; gap: 0.8rem; }
    .actions { margin: 1rem 0; }
    .grids { display: grid; grid-template-columns: 1.5fr 1fr; gap: 1.5rem; align-items: start; }
    .side { display: flex; flex-direction: column; gap: 1rem; }
    .totals { margin-top: 1rem; }
    .line { display: flex; justify-content: space-between; padding: 0.3rem 0; }
    .line.total { border-top: 1px solid var(--border); margin-top: 0.4rem; padding-top: 0.6rem; }
    .timeline { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 0.4rem; }
    @media (max-width: 760px) { .grids { grid-template-columns: 1fr; } }
  `],
})
export class OrderDetailPage implements OnInit {
  private readonly api = inject(OrdersApi);
  private readonly payments = inject(PaymentsApi);
  private readonly shipping = inject(ShippingApi);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<OrderDetail | null>(null);
  readonly tracking = signal<Tracking | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly busy = signal(false);

  readonly canPay = computed(() => {
    const s = this.order()?.status;
    return s === 'Pending' || s === 'PaymentPending' || s === 'PaymentFailed';
  });
  readonly canCancel = computed(() => {
    const s = this.order()?.status;
    return s === 'Pending' || s === 'PaymentPending' || s === 'PaymentFailed' || s === 'Paid';
  });

  ngOnInit(): void { this.reload(); }

  private reload(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.api.getMine(id).subscribe({
      next: (o) => {
        this.order.set(o);
        this.loading.set(false);
        this.shipping.shipmentForOrder(o.id).subscribe({ next: (t) => this.tracking.set(t), error: () => this.tracking.set(null) });
      },
      error: (err) => { this.error.set(problemMessage(err, 'Order not found.')); this.loading.set(false); },
    });
  }

  pay(o: OrderDetail): void {
    this.busy.set(true); this.actionError.set(null); this.actionMessage.set(null);
    this.payments.pay({ orderId: o.id }).subscribe({
      next: () => { this.actionMessage.set('Payment received.'); this.busy.set(false); this.reload(); },
      error: (err) => { this.actionError.set(problemMessage(err, 'Payment failed.')); this.busy.set(false); },
    });
  }

  cancel(o: OrderDetail): void {
    this.busy.set(true); this.actionError.set(null); this.actionMessage.set(null);
    this.api.cancel(o.id, { reason: 'Cancelled by customer' }).subscribe({
      next: () => { this.actionMessage.set('Order cancelled.'); this.busy.set(false); this.reload(); },
      error: (err) => { this.actionError.set(problemMessage(err, 'Could not cancel this order.')); this.busy.set(false); },
    });
  }
}
