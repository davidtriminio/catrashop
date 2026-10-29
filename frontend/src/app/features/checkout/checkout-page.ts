import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { CustomersApi } from '@ch/api/customers.api';
import { Address, CheckoutResponse, CouponQuote, ShippingMethod } from '@ch/api/models';
import { OrdersApi } from '@ch/api/orders.api';
import { PaymentsApi } from '@ch/api/payments.api';
import { PromotionsApi } from '@ch/api/promotions.api';
import { ShippingApi } from '@ch/api/shipping.api';
import { StatusBadge } from '@ch/ui/status-badge';
import { CartStore } from '../../core/cart/cart.store';
import { I18n } from '@ch/i18n/i18n';
import { MoneyPipe } from '@ch/util/money.pipe';

@Component({
  selector: 'app-checkout-page',
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe, StatusBadge, TranslocoPipe],
  template: `
    <h1>{{ 'checkout.title' | transloco }}</h1>

    @if (placed(); as order) {
      <div class="card confirm">
        <h2>{{ 'checkout.placed' | transloco: { number: order.orderNumber } }}</h2>
        <p>{{ 'checkout.status' | transloco }} <ch-status-badge [status]="order.status" /></p>
        <p>{{ 'checkout.totalDue' | transloco }} <strong>{{ order.grandTotal | money: order.currency }}</strong></p>
        @if (payError()) { <div class="alert error">{{ payError() }}</div> }
        <div class="row">
          <button class="btn" [disabled]="paying()" (click)="pay(order)">
            {{ (paying() ? 'checkout.processing' : 'orderDetail.payNow') | transloco }}
          </button>
          <a class="btn secondary" [routerLink]="['/orders', order.orderId]">{{ 'checkout.viewOrder' | transloco }}</a>
        </div>
      </div>
    } @else if (cart.cart(); as c) {
      @if (c.items.length === 0) {
        <div class="card"><p class="muted">{{ 'checkout.cartEmpty' | transloco }}</p><a routerLink="/products" class="btn">{{ 'cart.browse' | transloco }}</a></div>
      } @else {
        <div class="checkout">
          <form class="card" [formGroup]="form" (ngSubmit)="placeOrder()">
            <h2>{{ 'checkout.shippingAddress' | transloco }}</h2>

            @if (addresses().length) {
              <div class="field">
                <label for="saved">{{ 'checkout.savedAddress' | transloco }}</label>
                <select id="saved" (change)="prefill($event)">
                  <option value="">{{ 'checkout.newAddress' | transloco }}</option>
                  @for (a of addresses(); track a.id) { <option [value]="a.id">{{ a.label }} — {{ a.line1 }}, {{ a.city }}</option> }
                </select>
              </div>
            }

            <div class="field"><label>{{ 'checkout.recipientName' | transloco }}</label><input class="input" formControlName="recipientName" /></div>
            <div class="field"><label>{{ 'checkout.line1' | transloco }}</label><input class="input" formControlName="line1" /></div>
            <div class="field"><label>{{ 'checkout.line2' | transloco }}</label><input class="input" formControlName="line2" /></div>
            <div class="row">
              <div class="field" style="flex:2"><label>{{ 'account.city' | transloco }}</label><input class="input" formControlName="city" /></div>
              <div class="field" style="flex:1"><label>{{ 'account.region' | transloco }}</label><input class="input" formControlName="region" /></div>
            </div>
            <div class="row">
              <div class="field" style="flex:1"><label>{{ 'account.postalCode' | transloco }}</label><input class="input" formControlName="postalCode" /></div>
              <div class="field" style="flex:1"><label>{{ 'account.country' | transloco }}</label><input class="input" maxlength="2" formControlName="country" /></div>
            </div>

            @if (methods().length) {
              <div class="field">
                <label for="method">{{ 'checkout.shippingMethod' | transloco }}</label>
                <select id="method" [value]="selectedMethod()?.code ?? ''" (change)="selectMethod($event)">
                  @for (m of methods(); track m.id) {
                    <option [value]="m.code">{{ m.name }} — {{ m.cost | money: m.currency }} ({{ 'checkout.days' | transloco: { count: m.estimatedDays } }})</option>
                  }
                </select>
                <small class="muted">{{ 'checkout.estimateNote' | transloco }}</small>
              </div>
            }

            @if (error()) { <div class="alert error">{{ error() }}</div> }
            <button class="btn block" type="submit" [disabled]="form.invalid || placing()">
              {{ (placing() ? 'checkout.placing' : 'checkout.placeOrder') | transloco }}
            </button>
          </form>

          <aside class="summary card">
            <h2>{{ 'checkout.summary' | transloco }}</h2>
            <div class="line"><span>{{ 'orderDetail.subtotal' | transloco }}</span><span>{{ c.subtotal | money: c.currency }}</span></div>

            <div class="field coupon">
              <label for="coupon">{{ 'checkout.couponCode' | transloco }}</label>
              <div class="row">
                <input id="coupon" class="input" [value]="couponCode()" (input)="setCoupon($event)" [placeholder]="'checkout.couponPlaceholder' | transloco" />
                <button type="button" class="btn secondary" [disabled]="!couponCode() || checking()" (click)="applyCoupon(c.subtotal, c.currency)">{{ 'checkout.apply' | transloco }}</button>
              </div>
              @if (couponError()) { <div class="alert error">{{ couponError() }}</div> }
              @if (quote(); as qz) { <div class="alert success">{{ 'checkout.couponApplied' | transloco: { code: qz.couponCode, amount: (qz.discountAmount | money: qz.currency) } }}</div> }
            </div>

            @if (quote(); as qz) {
              <div class="line"><span>{{ 'orderDetail.discount' | transloco }}</span><span>−{{ qz.discountAmount | money: c.currency }}</span></div>
            }
            @if (selectedMethod(); as m) {
              <div class="line muted"><span>{{ 'checkout.shippingEstimate' | transloco }}</span><span>{{ m.cost | money: m.currency }}</span></div>
            }
            <p class="muted">{{ 'checkout.taxesNote' | transloco }}</p>
          </aside>
        </div>
      }
    } @else {
      <p class="muted">{{ 'common.loading' | transloco }}</p>
    }
  `,
  styles: [`
    .checkout { display: grid; grid-template-columns: 1.4fr 1fr; gap: 1.5rem; align-items: start; }
    .summary .line { display: flex; justify-content: space-between; padding: 0.35rem 0; }
    .coupon { margin: 1rem 0; }
    .confirm { max-width: 480px; }
    @media (max-width: 760px) { .checkout { grid-template-columns: 1fr; } }
  `],
})
export class CheckoutPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly i18n = inject(I18n);
  private readonly orders = inject(OrdersApi);
  private readonly payments = inject(PaymentsApi);
  private readonly promotions = inject(PromotionsApi);
  private readonly shipping = inject(ShippingApi);
  private readonly customers = inject(CustomersApi);
  readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  readonly addresses = signal<Address[]>([]);
  readonly methods = signal<ShippingMethod[]>([]);
  readonly selectedMethod = signal<ShippingMethod | null>(null);

  readonly couponCode = signal('');
  readonly quote = signal<CouponQuote | null>(null);
  readonly couponError = signal<string | null>(null);
  readonly checking = signal(false);

  readonly placing = signal(false);
  readonly error = signal<string | null>(null);
  readonly placed = signal<CheckoutResponse | null>(null);
  readonly paying = signal(false);
  readonly payError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    recipientName: ['', [Validators.required]],
    line1: ['', [Validators.required]],
    line2: [''],
    city: ['', [Validators.required]],
    region: [''],
    postalCode: ['', [Validators.required]],
    country: ['', [Validators.required, Validators.maxLength(2)]],
  });

  ngOnInit(): void {
    this.cart.refresh();
    this.customers.addresses().subscribe({ next: (a) => this.applyDefaultAddress(a), error: () => {} });
    this.shipping.methods().subscribe({
      next: (m) => { this.methods.set(m); if (m.length) this.selectedMethod.set(m[0]); },
      error: () => {},
    });
  }

  private applyDefaultAddress(addresses: Address[]): void {
    this.addresses.set(addresses);
    const def = addresses.find((a) => a.isDefaultShipping) ?? addresses[0];
    if (def) this.fillFrom(def);
  }

  prefill(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    const address = this.addresses().find((a) => a.id === id);
    if (address) this.fillFrom(address);
  }

  private fillFrom(a: Address): void {
    this.form.patchValue({
      recipientName: a.recipientName, line1: a.line1, line2: a.line2 ?? '', city: a.city,
      region: a.region ?? '', postalCode: a.postalCode, country: a.country,
    });
  }

  selectMethod(event: Event): void {
    const code = (event.target as HTMLSelectElement).value;
    this.selectedMethod.set(this.methods().find((m) => m.code === code) ?? null);
  }

  setCoupon(event: Event): void {
    this.couponCode.set((event.target as HTMLInputElement).value.trim());
    this.quote.set(null);
    this.couponError.set(null);
  }

  applyCoupon(subtotal: number, currency: string): void {
    this.checking.set(true);
    this.couponError.set(null);
    this.promotions.quote(this.couponCode(), subtotal, currency).subscribe({
      next: (q) => { this.quote.set(q); this.checking.set(false); },
      error: (err) => { this.quote.set(null); this.couponError.set(this.i18n.problem(err, 'checkout.couponInvalid')); this.checking.set(false); },
    });
  }

  placeOrder(): void {
    if (this.form.invalid) return;
    this.placing.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    this.orders.checkout({
      shippingAddress: {
        recipientName: v.recipientName, line1: v.line1, line2: v.line2 || null, city: v.city,
        region: v.region || null, postalCode: v.postalCode, country: v.country.toUpperCase(),
      },
      couponCode: this.quote() ? this.couponCode() : null,
    }).subscribe({
      next: (order) => { this.placed.set(order); this.cart.refresh(); this.placing.set(false); },
      error: (err) => { this.error.set(this.i18n.problem(err, 'checkout.failed')); this.placing.set(false); },
    });
  }

  pay(order: CheckoutResponse): void {
    this.paying.set(true);
    this.payError.set(null);
    this.payments.pay({ orderId: order.orderId }).subscribe({
      next: () => { this.cart.refresh(); this.router.navigate(['/orders', order.orderId]); },
      error: (err) => { this.payError.set(this.i18n.problem(err, 'orderDetail.payFailed')); this.paying.set(false); },
    });
  }
}
