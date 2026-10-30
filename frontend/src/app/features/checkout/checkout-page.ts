import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { CustomersApi } from '@cs/api/customers.api';
import { Address, CheckoutResponse, CouponQuote, PickupStore, ShippingZone } from '@cs/api/models';
import { OrdersApi } from '@cs/api/orders.api';
import { PaymentsApi } from '@cs/api/payments.api';
import { PromotionsApi } from '@cs/api/promotions.api';
import { ShippingApi } from '@cs/api/shipping.api';
import { I18n } from '@cs/i18n/i18n';
import { StatusBadge } from '@cs/ui/status-badge';
import { CartStore } from '../../core/cart/cart.store';
import { MoneyPipe } from '@cs/util/money.pipe';

type DeliveryMode = 'Delivery' | 'Pickup';

interface DepartmentOption { code: string; name: string; zone: ShippingZone; }

/** Lower-case, accent-free text, to match a saved address's free-text region against the department names. */
const plain = (text: string | null | undefined) =>
  (text ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').trim().toLowerCase();

@Component({
  selector: 'app-checkout-page',
  imports: [ReactiveFormsModule, RouterLink, MoneyPipe, StatusBadge, TranslocoPipe],
  template: `
    <h1>{{ 'checkout.title' | transloco }}</h1>

    @if (placed(); as order) {
      <div class="card confirm">
        <h2>{{ 'checkout.placed' | transloco: { number: order.orderNumber } }}</h2>
        <p>{{ 'checkout.status' | transloco }} <cs-status-badge [status]="order.status" /></p>
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
            <h2>{{ 'checkout.delivery' | transloco }}</h2>

            @if (stores().length) {
              <div class="modes" role="radiogroup" [attr.aria-label]="'checkout.delivery' | transloco">
                <label class="mode" [class.on]="mode() === 'Delivery'">
                  <input type="radio" name="mode" [checked]="mode() === 'Delivery'" (change)="mode.set('Delivery')" />
                  <span>{{ 'checkout.mode.delivery' | transloco }}</span>
                </label>
                <label class="mode" [class.on]="mode() === 'Pickup'">
                  <input type="radio" name="mode" [checked]="mode() === 'Pickup'" (change)="mode.set('Pickup')" />
                  <span>{{ 'checkout.mode.pickup' | transloco }}</span>
                </label>
              </div>
            }

            @if (mode() === 'Delivery') {
              <h3>{{ 'checkout.shippingAddress' | transloco }}</h3>

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
                <div class="field" style="flex:1"><label>{{ 'account.postalCode' | transloco }}</label><input class="input" formControlName="postalCode" /></div>
              </div>

              <div class="field">
                <label for="department">{{ 'checkout.department' | transloco }}</label>
                <select id="department" formControlName="departmentCode">
                  <option value="">{{ 'checkout.departmentPlaceholder' | transloco }}</option>
                  @for (d of departments(); track d.code) { <option [value]="d.code">{{ d.name }}</option> }
                </select>
                @if (zones().length === 0) {
                  <small class="muted">{{ 'checkout.noZones' | transloco }}</small>
                } @else if (selectedZone(); as z) {
                  <small class="muted">{{ zoneInfo(z) }}</small>
                }
              </div>
            } @else {
              <h3>{{ 'checkout.pickupTitle' | transloco }}</h3>
              <form [formGroup]="pickupForm">
                <div class="field">
                  <label for="store">{{ 'checkout.pickupStore' | transloco }}</label>
                  <select id="store" formControlName="storeId">
                    <option value="">{{ 'checkout.pickupStorePlaceholder' | transloco }}</option>
                    @for (s of stores(); track s.id) { <option [value]="s.id">{{ s.name }} — {{ s.city }}</option> }
                  </select>
                  @if (selectedStore(); as s) {
                    <small class="muted">{{ s.line1 }}, {{ s.city }}, {{ s.departmentName }}@if (s.hours) { · {{ 'checkout.pickupHours' | transloco: { hours: s.hours } }} }</small>
                  }
                </div>
                <div class="field">
                  <label for="collector">{{ 'checkout.pickupRecipient' | transloco }}</label>
                  <input id="collector" class="input" formControlName="recipientName" />
                </div>
              </form>
              <p class="muted">{{ 'checkout.pickupNote' | transloco }}</p>
            }

            @if (error()) { <div class="alert error">{{ error() }}</div> }
            <button class="btn block" type="submit" [disabled]="!canSubmit() || placing()">
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
            <div class="line">
              <span>{{ 'checkout.shipping' | transloco }}</span>
              @if (shippingCost() === null) { <span class="muted">{{ 'checkout.shippingPending' | transloco }}</span> }
              @else if (shippingCost() === 0) { <span>{{ 'checkout.free' | transloco }}</span> }
              @else { <span>{{ shippingCost() | money: c.currency }}</span> }
            </div>
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
    .modes { display: grid; grid-template-columns: 1fr 1fr; gap: var(--space-3); margin-bottom: var(--space-4); }
    .mode { display: flex; align-items: center; gap: var(--space-2); padding: var(--space-3) var(--space-4); border: 1px solid var(--border-strong); border-radius: var(--radius-sm); cursor: pointer; font-weight: 600; font-size: 0.92rem; }
    .mode.on { border-color: var(--primary); background: var(--surface-2); }
    .mode input { accent-color: var(--primary); margin: 0; }
    h3 { font-size: 0.95rem; margin: var(--space-4) 0 var(--space-3); }
    @media (max-width: 760px) { .checkout { grid-template-columns: 1fr; } }
    @media (max-width: 420px) { .modes { grid-template-columns: 1fr; } }
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
  readonly zones = signal<ShippingZone[]>([]);
  readonly stores = signal<PickupStore[]>([]);
  readonly mode = signal<DeliveryMode>('Delivery');

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
    postalCode: ['', [Validators.required]],
    departmentCode: ['', [Validators.required]],
  });

  readonly pickupForm = this.fb.nonNullable.group({
    storeId: ['', [Validators.required]],
    recipientName: ['', [Validators.required, Validators.maxLength(150)]],
  });

  /** The form's department changes outside Angular's signals, so mirror it into one the summary can read. */
  private readonly departmentCode = signal('');
  private readonly storeId = signal('');

  readonly departments = computed<DepartmentOption[]>(() =>
    this.zones()
      .flatMap((zone) => zone.departments.map((d) => ({ code: d.code, name: d.name, zone })))
      .sort((a, b) => a.name.localeCompare(b.name, 'es')));

  readonly selectedZone = computed(() => this.departments().find((d) => d.code === this.departmentCode())?.zone ?? null);
  readonly selectedStore = computed(() => this.stores().find((s) => s.id === this.storeId()) ?? null);

  /** The delivery cost: 0 for a pickup, the destination zone's cost for a delivery, null until a department is chosen. */
  readonly shippingCost = computed<number | null>(() =>
    this.mode() === 'Pickup' ? 0 : (this.selectedZone()?.cost ?? null));

  /** A method, not a `computed`: form validity is not a signal, so it must be read on every change detection. */
  canSubmit(): boolean {
    return this.mode() === 'Delivery' ? this.form.valid : this.pickupForm.valid;
  }

  ngOnInit(): void {
    this.cart.refresh();
    this.form.controls.departmentCode.valueChanges.subscribe((v) => this.departmentCode.set(v));
    this.pickupForm.controls.storeId.valueChanges.subscribe((v) => this.storeId.set(v));
    this.customers.addresses().subscribe({ next: (a) => this.applyDefaultAddress(a), error: () => {} });
    this.shipping.zones().subscribe({ next: (z) => { this.zones.set(z); this.matchDepartmentOfSavedAddress(); }, error: () => {} });
    this.shipping.pickupStores().subscribe({ next: (s) => this.stores.set(s), error: () => {} });
  }

  zoneInfo(zone: ShippingZone): string {
    const days = zone.estimatedDaysMin === zone.estimatedDaysMax
      ? this.i18n.t(`checkout.daysSingle.${zone.estimatedDaysMax === 1 ? 'one' : 'other'}`, { days: zone.estimatedDaysMax })
      : this.i18n.t('checkout.daysRange', { min: zone.estimatedDaysMin, max: zone.estimatedDaysMax });
    const cost = zone.cost === 0 ? this.i18n.t('checkout.free') : this.formatCost(zone);
    return this.i18n.t('checkout.zoneInfo', { zone: zone.name, cost, days });
  }

  private formatCost(zone: ShippingZone): string {
    return `LPS. ${zone.cost.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
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
      postalCode: a.postalCode, departmentCode: this.departmentOf(a.region),
    });
  }

  /** Saved addresses keep the department as free text; pick the matching department when its name is the same. */
  private departmentOf(region: string | null): string {
    const wanted = plain(region);
    return wanted ? (this.departments().find((d) => plain(d.name) === wanted)?.code ?? '') : '';
  }

  private matchDepartmentOfSavedAddress(): void {
    if (this.form.controls.departmentCode.value) return;
    const def = this.addresses().find((a) => a.isDefaultShipping) ?? this.addresses()[0];
    if (def) this.form.patchValue({ departmentCode: this.departmentOf(def.region) });
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
    if (!this.canSubmit()) return;
    this.placing.set(true);
    this.error.set(null);
    const couponCode = this.quote() ? this.couponCode() : null;

    const request = this.mode() === 'Pickup'
      ? (() => {
          const p = this.pickupForm.getRawValue();
          return { shippingAddress: null, couponCode, deliveryMode: 'Pickup' as const, pickupStoreId: p.storeId, pickupRecipientName: p.recipientName.trim() };
        })()
      : (() => {
          const v = this.form.getRawValue();
          return {
            shippingAddress: {
              recipientName: v.recipientName, line1: v.line1, line2: v.line2 || null, city: v.city, region: null,
              postalCode: v.postalCode, country: 'HN', departmentCode: v.departmentCode,
            },
            couponCode, deliveryMode: 'Delivery' as const,
          };
        })();

    this.orders.checkout(request).subscribe({
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
