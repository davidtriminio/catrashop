import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OrdersAdminApi } from '@cs/api/orders-admin.api';
import { OrderDetail, PaymentResponse } from '@cs/api/models';
import { PaymentsAdminApi } from '@cs/api/payments-admin.api';
import { StatusBadge } from '@cs/ui/status-badge';
import { Timeline } from '@cs/ui/timeline';
import { MoneyPipe } from '@cs/util/money.pipe';
import { problemMessage } from '@cs/util/problem';

type Action = 'process' | 'ship' | 'deliver' | 'cancel';

@Component({
  selector: 'app-order-detail-admin',
  imports: [DatePipe, RouterLink, MoneyPipe, StatusBadge, Timeline],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    @if (order(); as o) {
      <nav class="crumbs" aria-label="Breadcrumb">
        <a routerLink="/orders">Orders</a><span>/</span><span aria-current="page">{{ o.orderNumber }}</span>
      </nav>

      <div class="page-head">
        <div class="title">
          <h1>{{ o.orderNumber }}</h1>
          <cs-status-badge [status]="o.status" />
        </div>
        <div class="actions">
          @if (next(o.status); as n) {
            <button class="btn" [disabled]="busy()" (click)="act(n.action, o)">{{ n.label }}</button>
          }
          @if (canCancel(o.status)) {
            <button class="btn secondary danger-text" [disabled]="busy()" (click)="act('cancel', o)">Cancel order</button>
          }
        </div>
      </div>
      <p class="muted placed">Customer {{ o.customerId }} · placed {{ o.createdAtUtc | date: 'MMMM d, y, h:mm a' }}</p>

      <div class="cols grids">
        <div class="card">
          <h2>Items</h2>
          @for (i of o.items; track i.id) {
            <div class="item">
              <div class="what"><strong>{{ i.productName }}</strong><span class="muted">{{ i.sku }} · {{ i.quantity }} × {{ i.unitPrice | money: o.currency }}</span></div>
              <strong>{{ i.lineTotal | money: o.currency }}</strong>
            </div>
          }
          <div class="totals">
            <div class="line"><span class="muted">Subtotal</span><span>{{ o.subtotal | money: o.currency }}</span></div>
            @if (o.discountTotal > 0) { <div class="line"><span class="muted">Discount</span><span>−{{ o.discountTotal | money: o.currency }}</span></div> }
            <div class="line"><span class="muted">Tax</span><span>{{ o.taxTotal | money: o.currency }}</span></div>
            <div class="line"><span class="muted">Shipping</span><span>{{ o.shippingTotal | money: o.currency }}</span></div>
            <div class="line total"><span>Total</span><strong>{{ o.grandTotal | money: o.currency }}</strong></div>
          </div>
        </div>

        <div class="side">
          <div class="card">
            <h2>{{ o.deliveryMode === 'Pickup' ? 'Pickup at store' : 'Ship to' }}</h2>
            <p class="addr">
              <strong>{{ o.shippingAddress.recipientName }}</strong><br />
              {{ o.shippingAddress.line1 }}@if (o.shippingAddress.line2) { , {{ o.shippingAddress.line2 }} }<br />
              {{ o.shippingAddress.city }}@if (o.shippingAddress.postalCode !== '-') { , {{ o.shippingAddress.postalCode }} }<br />
              {{ o.shippingAddress.country }}
            </p>
          </div>
          @if (o.paymentMethod) {
            <div class="card">
              <h2>Payment</h2>
              <p class="line"><span class="muted">Method</span><strong>{{ methodLabel(o.paymentMethod) }}</strong></p>
              @if (payment(); as p) {
                <p class="line"><span class="muted">Status</span><cs-status-badge [status]="p.status" /></p>
                @if (p.transferReference) { <p class="line"><span class="muted">Transfer</span><span>{{ p.transferBankAccountCode }} · {{ p.transferReference }}</span></p> }
                @if (p.status === 'UnderReview') { <a class="btn secondary sm" routerLink="/payments">Verify the receipt</a> }
                @if (p.status === 'Pending' && o.paymentMethod !== 'TRANSFERENCIA') {
                  @if (o.status === 'Delivered') {
                    <button class="btn sm" [disabled]="busy()" (click)="collect(p)">Mark money collected</button>
                  } @else { <p class="muted">The money is collected once the order is delivered.</p> }
                }
              } @else { <p class="muted">The customer has not started the payment.</p> }
            </div>
          }
          <div class="card">
            <h2>History</h2>
            <cs-timeline [items]="o.history" />
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .crumbs { display: flex; flex-wrap: wrap; gap: var(--space-2); font-size: 0.82rem; color: var(--muted); margin-bottom: var(--space-3); }
    .crumbs a:hover { color: var(--text); }
    .title { display: flex; align-items: center; gap: var(--space-3); flex-wrap: wrap; }
    .actions { display: flex; gap: var(--space-2); flex-wrap: wrap; }
    .danger-text { color: var(--danger); }
    .placed { margin-top: calc(var(--space-3) * -1); margin-bottom: var(--space-5); }
    .grids { display: grid; grid-template-columns: minmax(0, 1.5fr) minmax(0, 1fr); gap: var(--space-5); align-items: start; }
    .side { display: flex; flex-direction: column; gap: var(--space-5); }
    .side > .card + .card { margin-top: 0; }
    .item { display: flex; justify-content: space-between; align-items: center; gap: var(--space-3); padding: var(--space-3) 0; border-bottom: 1px solid var(--border); }
    .what { display: flex; flex-direction: column; min-width: 0; }
    .what .muted { font-size: 0.82rem; }
    .totals { margin-top: var(--space-4); }
    .line { display: flex; justify-content: space-between; padding: var(--space-1) 0; }
    .line.total { border-top: 1px solid var(--border); margin-top: var(--space-2); padding-top: var(--space-3); font-size: 1.05rem; }
    .addr { margin: 0; line-height: 1.6; }
    .side .line { justify-content: flex-start; align-items: center; gap: var(--space-3); }
    @media (max-width: 760px) { .grids { grid-template-columns: 1fr; } }
  `],
})
export class OrderDetailAdmin implements OnInit {
  private readonly api = inject(OrdersAdminApi);
  private readonly payments = inject(PaymentsAdminApi);
  private readonly route = inject(ActivatedRoute);

  readonly order = signal<OrderDetail | null>(null);
  readonly payment = signal<PaymentResponse | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);
  private id = computed(() => this.route.snapshot.paramMap.get('id')!);

  ngOnInit(): void { this.load(); }
  private load(): void {
    this.api.get(this.id()).subscribe({
      next: (o) => { this.order.set(o); this.loadPayment(o); },
      error: (e) => this.error.set(problemMessage(e, 'Order not found.')),
    });
  }

  private loadPayment(o: OrderDetail): void {
    if (!o.paymentMethod) { this.payment.set(null); return; }
    this.payments.forOrder(o.id).subscribe({ next: (p) => this.payment.set(p), error: () => this.payment.set(null) });
  }

  methodLabel(method: string): string {
    return { CONTRA_ENTREGA: 'Cash on delivery', TRANSFERENCIA: 'Bank transfer', PAGO_EN_TIENDA: 'Pay at the store' }[method] ?? method;
  }

  collect(p: PaymentResponse): void {
    this.busy.set(true); this.error.set(null); this.message.set(null);
    this.payments.collect(p.id).subscribe({
      next: () => { this.message.set('Payment registered as collected.'); this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(problemMessage(e)); this.busy.set(false); },
    });
  }

  /** The one fulfilment step the order state machine allows next (null when there is none). */
  next(status: string): { action: Action; label: string } | null {
    switch (status) {
      case 'Paid': case 'Confirmed': return { action: 'process', label: 'Start processing' };
      case 'Processing': return { action: 'ship', label: 'Mark shipped' };
      case 'Shipped': return { action: 'deliver', label: 'Mark delivered' };
      default: return null;
    }
  }

  canCancel(status: string): boolean {
    return ['Pending', 'PaymentPending', 'PaymentFailed', 'Paid', 'Confirmed', 'Processing'].includes(status);
  }

  act(action: Action, o: OrderDetail): void {
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
