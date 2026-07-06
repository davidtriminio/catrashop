import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CustomersApi } from '../../core/api/customers.api';
import { Address, CheckoutResponse, CouponQuote, ShippingMethod } from '../../core/api/models';
import { OrdersApi } from '../../core/api/orders.api';
import { PaymentsApi } from '../../core/api/payments.api';
import { PromotionsApi } from '../../core/api/promotions.api';
import { ShippingApi } from '../../core/api/shipping.api';
import { CartStore } from '../../core/cart/cart.store';
import { MoneyPipe } from '../../shared/money.pipe';
import { problemMessage } from '../../shared/problem';

@Component({
  selector: 'app-checkout-page',
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe],
  template: `
    <h1>Checkout</h1>

    @if (placed(); as order) {
      <div class="card confirm">
        <h2>Order {{ order.orderNumber }} placed</h2>
        <p>Status: <span class="badge warn">{{ order.status }}</span></p>
        <p>Total due: <strong>{{ order.grandTotal | money: order.currency }}</strong></p>
        @if (payError()) { <div class="alert error">{{ payError() }}</div> }
        <div class="row">
          <button class="btn" [disabled]="paying()" (click)="pay(order)">
            {{ paying() ? 'Processing…' : 'Pay now' }}
          </button>
          <a class="btn secondary" [routerLink]="['/orders', order.orderId]">View order</a>
        </div>
      </div>
    } @else if (cart.cart(); as c) {
      @if (c.items.length === 0) {
        <div class="card"><p class="muted">Your cart is empty.</p><a routerLink="/products" class="btn">Browse products</a></div>
      } @else {
        <div class="checkout">
          <form class="card" [formGroup]="form" (ngSubmit)="placeOrder()">
            <h2>Shipping address</h2>

            @if (addresses().length) {
              <div class="field">
                <label for="saved">Use a saved address</label>
                <select id="saved" (change)="prefill($event)">
                  <option value="">— New address —</option>
                  @for (a of addresses(); track a.id) { <option [value]="a.id">{{ a.label }} — {{ a.line1 }}, {{ a.city }}</option> }
                </select>
              </div>
            }

            <div class="field"><label>Recipient name</label><input class="input" formControlName="recipientName" /></div>
            <div class="field"><label>Address line 1</label><input class="input" formControlName="line1" /></div>
            <div class="field"><label>Address line 2 (optional)</label><input class="input" formControlName="line2" /></div>
            <div class="row">
              <div class="field" style="flex:2"><label>City</label><input class="input" formControlName="city" /></div>
              <div class="field" style="flex:1"><label>Region</label><input class="input" formControlName="region" /></div>
            </div>
            <div class="row">
              <div class="field" style="flex:1"><label>Postal code</label><input class="input" formControlName="postalCode" /></div>
              <div class="field" style="flex:1"><label>Country (ISO-2)</label><input class="input" maxlength="2" formControlName="country" /></div>
            </div>

            @if (methods().length) {
              <div class="field">
                <label for="method">Shipping method (estimate)</label>
                <select id="method" [value]="selectedMethod()?.code ?? ''" (change)="selectMethod($event)">
                  @for (m of methods(); track m.id) {
                    <option [value]="m.code">{{ m.name }} — {{ m.cost | money: m.currency }} ({{ m.estimatedDays }}d)</option>
                  }
                </select>
                <small class="muted">Shown as an estimate; the order total is confirmed by the server.</small>
              </div>
            }

            @if (error()) { <div class="alert error">{{ error() }}</div> }
            <button class="btn block" type="submit" [disabled]="form.invalid || placing()">
              {{ placing() ? 'Placing order…' : 'Place order' }}
            </button>
          </form>

          <aside class="summary card">
            <h2>Summary</h2>
            <div class="line"><span>Subtotal</span><span>{{ c.subtotal | money: c.currency }}</span></div>

            <div class="field coupon">
              <label for="coupon">Coupon code</label>
              <div class="row">
                <input id="coupon" class="input" [value]="couponCode()" (input)="setCoupon($event)" placeholder="e.g. SAVE10" />
                <button type="button" class="btn secondary" [disabled]="!couponCode() || checking()" (click)="applyCoupon(c.subtotal, c.currency)">Apply</button>
              </div>
              @if (couponError()) { <div class="alert error">{{ couponError() }}</div> }
              @if (quote(); as qz) { <div class="alert success">Coupon {{ qz.couponCode }}: −{{ qz.discountAmount | money: qz.currency }}</div> }
            </div>

            @if (quote(); as qz) {
              <div class="line"><span>Discount</span><span>−{{ qz.discountAmount | money: c.currency }}</span></div>
            }
            @if (selectedMethod(); as m) {
              <div class="line muted"><span>Shipping (est.)</span><span>{{ m.cost | money: m.currency }}</span></div>
            }
            <p class="muted">Taxes are calculated on the confirmed order.</p>
          </aside>
        </div>
      }
    } @else {
      <p class="muted">Loading…</p>
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
      error: (err) => { this.quote.set(null); this.couponError.set(problemMessage(err, 'Coupon is not valid.')); this.checking.set(false); },
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
      error: (err) => { this.error.set(problemMessage(err, 'Checkout failed.')); this.placing.set(false); },
    });
  }

  pay(order: CheckoutResponse): void {
    this.paying.set(true);
    this.payError.set(null);
    this.payments.pay({ orderId: order.orderId }).subscribe({
      next: () => { this.cart.refresh(); this.router.navigate(['/orders', order.orderId]); },
      error: (err) => { this.payError.set(problemMessage(err, 'Payment failed.')); this.paying.set(false); },
    });
  }
}
