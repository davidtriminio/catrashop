import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { BusyButton } from '../ui/busy-button';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { LocalDatePipe } from '@cs/util/local-date.pipe';
import { OrdersAdminApi } from '@cs/api/orders-admin.api';
import { OrderDetail, PaymentResponse } from '@cs/api/models';
import { PaymentsAdminApi } from '@cs/api/payments-admin.api';
import { StatusBadge } from '@cs/ui/status-badge';
import { Timeline } from '@cs/ui/timeline';
import { MoneyPipe } from '@cs/util/money.pipe';

type Action = 'process' | 'ship' | 'deliver' | 'cancel';

@Component({
  selector: 'app-order-detail-admin',
  imports: [LocalDatePipe, RouterLink, MoneyPipe, StatusBadge, Timeline, TranslocoPipe, BusyButton],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    @if (!order() && !error()) {
      <div role="status" aria-busy="true">
        <span class="skeleton sk-text" style="width: 8rem; margin-bottom: var(--space-3)"></span>
        <span class="skeleton sk-title"></span>
        <span class="skeleton sk-text" style="width: 18rem; margin-bottom: var(--space-5)"></span>
        <div class="cols grids">
          <div class="card">
            @for (n of [1, 2, 3]; track n) { <div class="item"><span class="skeleton sk-text" style="width: 55%"></span><span class="skeleton sk-text" style="width: 4rem"></span></div> }
            <span class="skeleton sk-text" style="width: 40%; margin-top: var(--space-4)"></span>
          </div>
          <div class="side">
            <div class="card"><span class="skeleton sk-text" style="width: 60%"></span><span class="skeleton sk-text" style="width: 80%; margin-top: var(--space-3)"></span><span class="skeleton sk-text" style="width: 50%; margin-top: var(--space-2)"></span></div>
            <div class="card"><span class="skeleton sk-text" style="width: 45%"></span><span class="skeleton sk-text" style="width: 70%; margin-top: var(--space-3)"></span></div>
          </div>
        </div>
      </div>
    }

    @if (order(); as o) {
      <nav class="crumbs" [aria-label]="'admin.orderDetail.breadcrumb' | transloco">
        <a routerLink="/orders">{{ 'admin.nav.orders' | transloco }}</a><span>/</span><span aria-current="page">{{ o.orderNumber }}</span>
      </nav>

      <div class="page-head">
        <div class="title">
          <h1>{{ o.orderNumber }}</h1>
          <cs-status-badge [status]="o.status" />
        </div>
        <div class="actions">
          @if (next(o.status); as n) {
            <button class="btn" [csBusy]="busy()" [disabled]="busy()" (click)="act(n.action, o)">{{ n.label | transloco }}</button>
          }
          @if (canCancel(o.status)) {
            <button class="btn secondary danger-text" [csBusy]="busy()" [disabled]="busy()" (click)="act('cancel', o)">{{ 'admin.orderDetail.cancelOrder' | transloco }}</button>
          }
        </div>
      </div>
      <p class="muted placed">{{ 'admin.orderDetail.customer' | transloco }} {{ o.customerId }} · {{ 'admin.orderDetail.placed' | transloco }} {{ o.createdAtUtc | localDate: 'long' }}</p>

      <div class="cols grids">
        <div class="card">
          <h2>{{ 'admin.orderDetail.items' | transloco }}</h2>
          @for (i of o.items; track i.id) {
            <div class="item">
              <div class="what"><strong>{{ i.productName }}</strong><span class="muted">{{ i.sku }} · {{ i.quantity }} × {{ i.unitPrice | money: o.currency }}</span></div>
              <strong>{{ i.lineTotal | money: o.currency }}</strong>
            </div>
          }
          <div class="totals">
            <div class="line"><span class="muted">{{ 'admin.orderDetail.subtotal' | transloco }}</span><span>{{ o.subtotal | money: o.currency }}</span></div>
            @if (o.discountTotal > 0) { <div class="line"><span class="muted">{{ 'admin.orderDetail.discount' | transloco }}</span><span>−{{ o.discountTotal | money: o.currency }}</span></div> }
            <div class="line"><span class="muted">{{ 'admin.orderDetail.tax' | transloco }}</span><span>{{ o.taxTotal | money: o.currency }}</span></div>
            <div class="line"><span class="muted">{{ 'admin.orderDetail.shipping' | transloco }}</span><span>{{ o.shippingTotal | money: o.currency }}</span></div>
            <div class="line total"><span>{{ 'admin.c.total' | transloco }}</span><strong>{{ o.grandTotal | money: o.currency }}</strong></div>
          </div>
        </div>

        <div class="side">
          <div class="card">
            <h2>{{ (o.deliveryMode === 'Pickup' ? 'admin.orderDetail.pickup' : 'admin.orderDetail.shipTo') | transloco }}</h2>
            <p class="addr">
              <strong>{{ o.shippingAddress.recipientName }}</strong><br />
              {{ o.shippingAddress.line1 }}@if (o.shippingAddress.line2) { , {{ o.shippingAddress.line2 }} }<br />
              {{ o.shippingAddress.city }}@if (o.shippingAddress.postalCode !== '-') { , {{ o.shippingAddress.postalCode }} }<br />
              {{ o.shippingAddress.country }}
            </p>
          </div>
          @if (o.paymentMethod) {
            <div class="card">
              <h2>{{ 'admin.orderDetail.payment' | transloco }}</h2>
              <p class="line"><span class="muted">{{ 'admin.c.method' | transloco }}</span><strong>{{ methodLabel(o.paymentMethod) }}</strong></p>
              @if (payment(); as p) {
                <p class="line"><span class="muted">{{ 'admin.c.status' | transloco }}</span><cs-status-badge [status]="p.status" /></p>
                @if (p.transferReference) { <p class="line"><span class="muted">{{ 'admin.orderDetail.transfer' | transloco }}</span><span>{{ p.transferBankAccountCode }} · {{ p.transferReference }}</span></p> }
                @if (p.status === 'UnderReview') { <a class="btn secondary sm" routerLink="/payments">{{ 'admin.orderDetail.verify' | transloco }}</a> }
                @if (p.status === 'Pending' && o.paymentMethod !== 'TRANSFERENCIA') {
                  @if (o.status === 'Delivered') {
                    <button class="btn sm" [csBusy]="busy()" [disabled]="busy()" (click)="collect(p)">{{ 'admin.orderDetail.markCollected' | transloco }}</button>
                  } @else { <p class="muted">{{ 'admin.orderDetail.collectedHint' | transloco }}</p> }
                }
              } @else { <p class="muted">{{ 'admin.orderDetail.notStarted' | transloco }}</p> }
            </div>
          }
          <div class="card">
            <h2>{{ 'admin.orderDetail.history' | transloco }}</h2>
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
  private readonly i18n = inject(I18n);

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
      error: (e) => this.error.set(this.i18n.problem(e, 'admin.orderDetail.notFound')),
    });
  }

  private loadPayment(o: OrderDetail): void {
    if (!o.paymentMethod) { this.payment.set(null); return; }
    this.payments.forOrder(o.id).subscribe({ next: (p) => this.payment.set(p), error: () => this.payment.set(null) });
  }

  methodLabel(method: string): string {
    this.i18n.lang(); // recompute when the language changes
    return this.i18n.tOr(`admin.method.${method}`, method);
  }

  collect(p: PaymentResponse): void {
    this.busy.set(true); this.error.set(null); this.message.set(null);
    this.payments.collect(p.id).subscribe({
      next: () => { this.message.set(this.i18n.t('admin.orderDetail.collectedDone')); this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.busy.set(false); },
    });
  }

  /** The one fulfilment step the order state machine allows next (null when there is none). */
  next(status: string): { action: Action; label: string } | null {
    switch (status) {
      case 'Paid': case 'Confirmed': return { action: 'process', label: 'admin.orderDetail.start' };
      case 'Processing': return { action: 'ship', label: 'admin.orderDetail.markShipped' };
      case 'Shipped': return { action: 'deliver', label: 'admin.orderDetail.markDelivered' };
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
      : this.api.cancel(o.id, this.i18n.t('admin.orderDetail.cancelReason'));
    op.subscribe({
      next: () => { this.message.set(this.i18n.t('admin.orderDetail.updated')); this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.busy.set(false); },
    });
  }
}
