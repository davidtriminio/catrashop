import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrderDetail, Tracking } from '@ch/api/models';
import { OrdersApi } from '@ch/api/orders.api';
import { PaymentsApi } from '@ch/api/payments.api';
import { ShippingApi } from '@ch/api/shipping.api';
import { StatusBadge } from '@ch/ui/status-badge';
import { Timeline } from '@ch/ui/timeline';
import { I18n } from '@ch/i18n/i18n';
import { LocalDatePipe } from '@ch/util/local-date.pipe';
import { MoneyPipe } from '@ch/util/money.pipe';

@Component({
  selector: 'app-order-detail',
  imports: [LocalDatePipe, RouterLink, MoneyPipe, StatusBadge, Timeline, TranslocoPipe],
  template: `
    @if (loading()) { <div class="card"><div class="skeleton" style="height:1.4rem;width:40%"></div></div> }
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    @if (order(); as o) {
      <nav class="crumbs" [attr.aria-label]="'orderDetail.breadcrumb' | transloco">
        <a routerLink="/orders">{{ 'nav.orders' | transloco }}</a><span>/</span><span aria-current="page">{{ o.orderNumber }}</span>
      </nav>

      <div class="page-head">
        <div class="title">
          <h1>{{ o.orderNumber }}</h1>
          <ch-status-badge [status]="o.status" />
        </div>
        <div class="actions">
          @if (canPay()) { <button class="btn" [disabled]="busy()" (click)="pay(o)">{{ 'orderDetail.payNow' | transloco }}</button> }
          @if (canCancel()) { <button class="btn secondary danger-text" [disabled]="busy()" (click)="cancel(o)">{{ 'orderDetail.cancel' | transloco }}</button> }
        </div>
      </div>
      <p class="muted placed">{{ 'orderDetail.placed' | transloco: { date: (o.createdAtUtc | localDate: 'long') } }}</p>

      @if (actionError()) { <div class="alert error">{{ actionError() }}</div> }
      @if (actionMessage()) { <div class="alert success">{{ actionMessage() }}</div> }

      <div class="cols grids">
        <div class="card">
          <h2>{{ 'orderDetail.items' | transloco }}</h2>
          @for (item of o.items; track item.id) {
            <div class="item">
              <div class="thumb" aria-hidden="true">{{ item.productName.charAt(0) }}</div>
              <div class="what">
                <strong>{{ item.productName }}</strong>
                <span class="muted">{{ item.sku }} · {{ item.quantity }} × {{ item.unitPrice | money: o.currency }}</span>
              </div>
              <strong>{{ item.lineTotal | money: o.currency }}</strong>
            </div>
          }
          <div class="totals">
            <div class="line"><span class="muted">{{ 'orderDetail.subtotal' | transloco }}</span><span>{{ o.subtotal | money: o.currency }}</span></div>
            @if (o.discountTotal > 0) { <div class="line"><span class="muted">{{ 'orderDetail.discount' | transloco }}</span><span>−{{ o.discountTotal | money: o.currency }}</span></div> }
            <div class="line"><span class="muted">{{ 'orderDetail.tax' | transloco }}</span><span>{{ o.taxTotal | money: o.currency }}</span></div>
            <div class="line total"><span>{{ 'orderDetail.total' | transloco }}</span><strong>{{ o.grandTotal | money: o.currency }}</strong></div>
          </div>
        </div>

        <div class="side">
          <div class="card">
            <h2>{{ 'orderDetail.shipTo' | transloco }}</h2>
            <p class="addr">
              <strong>{{ o.shippingAddress.recipientName }}</strong><br />
              {{ o.shippingAddress.line1 }}@if (o.shippingAddress.line2) { , {{ o.shippingAddress.line2 }} }<br />
              {{ o.shippingAddress.city }}, {{ o.shippingAddress.postalCode }}<br />
              {{ o.shippingAddress.country }}
            </p>
          </div>

          @if (tracking(); as t) {
            <div class="card">
              <h2>{{ 'orderDetail.shipment' | transloco }}</h2>
              <p class="track"><span class="muted">{{ 'orderDetail.tracking' | transloco }}</span> <strong>{{ t.trackingNumber }}</strong> <ch-status-badge [status]="t.status" /></p>
              <ch-timeline [items]="t.events" />
            </div>
          }

          <div class="card">
            <h2>{{ 'orderDetail.history' | transloco }}</h2>
            <ch-timeline [items]="o.history" />
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
    .item { display: grid; grid-template-columns: 48px minmax(0, 1fr) auto; align-items: center; gap: var(--space-3); padding: var(--space-3) 0; border-bottom: 1px solid var(--border); }
    .thumb { width: 48px; height: 48px; border-radius: var(--radius-sm); background: #ece9f4; display: flex; align-items: center; justify-content: center; font-weight: 700; color: rgba(11, 11, 12, 0.25); }
    .what { display: flex; flex-direction: column; min-width: 0; }
    .what .muted { font-size: 0.82rem; }
    .totals { margin-top: var(--space-4); }
    .line { display: flex; justify-content: space-between; padding: var(--space-1) 0; }
    .line.total { border-top: 1px solid var(--border); margin-top: var(--space-2); padding-top: var(--space-3); font-size: 1.05rem; }
    .addr { margin: 0; line-height: 1.6; }
    .track { display: flex; align-items: center; flex-wrap: wrap; gap: var(--space-2); margin-bottom: var(--space-4); }
    @media (max-width: 760px) { .grids { grid-template-columns: 1fr; } }
  `],
})
export class OrderDetailPage implements OnInit {
  private readonly api = inject(OrdersApi);
  private readonly i18n = inject(I18n);
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
      error: (err) => { this.error.set(this.i18n.problem(err, 'orderDetail.notFound')); this.loading.set(false); },
    });
  }

  pay(o: OrderDetail): void {
    this.busy.set(true); this.actionError.set(null); this.actionMessage.set(null);
    this.payments.pay({ orderId: o.id }).subscribe({
      next: () => { this.actionMessage.set(this.i18n.t('orderDetail.paid')); this.busy.set(false); this.reload(); },
      error: (err) => { this.actionError.set(this.i18n.problem(err, 'orderDetail.payFailed')); this.busy.set(false); },
    });
  }

  cancel(o: OrderDetail): void {
    this.busy.set(true); this.actionError.set(null); this.actionMessage.set(null);
    this.api.cancel(o.id, { reason: 'Cancelled by customer' }).subscribe({
      next: () => { this.actionMessage.set(this.i18n.t('orderDetail.cancelled')); this.busy.set(false); this.reload(); },
      error: (err) => { this.actionError.set(this.i18n.problem(err, 'orderDetail.cancelFailed')); this.busy.set(false); },
    });
  }
}
