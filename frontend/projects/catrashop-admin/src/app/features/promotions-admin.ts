import { STORE_CURRENCY } from '@cs/util/store';
import { BusyButton } from '../ui/busy-button';
import { TableSkeleton } from '../ui/skeleton';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PromotionsAdminApi } from '@cs/api/promotions-admin.api';
import { Coupon } from '@cs/api/models';
import { MoneyPipe } from '@cs/util/money.pipe';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { Select } from '../ui/select';

@Component({
  selector: 'app-promotions-admin',
  imports: [ReactiveFormsModule, MoneyPipe, Select, TranslocoPipe, BusyButton, TableSkeleton],
  template: `
    <h1>{{ 'admin.nav.promotions' | transloco }}</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>{{ 'admin.promotions.newCoupon' | transloco }}</h2>
      <form [formGroup]="form" (ngSubmit)="create()">
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.code' | transloco }}</label><input class="input" formControlName="code" /></div>
          <div class="field" style="flex:2"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.type' | transloco }}</label>
            <cs-select formControlName="discountType" [options]="discountTypes()" />
          </div>
          <div class="field" style="flex:1"><label>{{ 'admin.promotions.value' | transloco }}</label><input class="input" type="number" min="0.01" step="0.01" formControlName="discountValue" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.c.currency' | transloco }}</label><input class="input" maxlength="3" formControlName="currency" readonly [title]="'admin.c.oneCurrency' | transloco" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.promotions.minSubtotal' | transloco }}</label><input class="input" type="number" min="0" step="0.01" formControlName="minimumSubtotal" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.promotions.usageLimit' | transloco }}</label><input class="input" type="number" min="1" formControlName="usageLimit" placeholder="∞" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.promotions.perCustomer' | transloco }}</label><input class="input" type="number" min="1" formControlName="perCustomerLimit" placeholder="∞" /></div>
        </div>
        <button class="btn" [csBusy]="busy()" [disabled]="form.invalid || busy()">{{ 'admin.promotions.create' | transloco }}</button>
      </form>
    </section>

    <section class="card">
      <h2>{{ 'admin.promotions.coupons' | transloco }}</h2>
      @if (!loaded()) { <app-table-skeleton [rows]="4" [cols]="6" /> } @else {
      <table>
        <thead><tr><th>{{ 'admin.c.code' | transloco }}</th><th>{{ 'admin.c.type' | transloco }}</th><th>{{ 'admin.promotions.value' | transloco }}</th><th>{{ 'admin.promotions.redeemed' | transloco }}</th><th>{{ 'admin.promotions.active' | transloco }}</th><th></th></tr></thead>
        <tbody>
          @for (c of coupons(); track c.id) {
            <tr>
              <td><strong>{{ c.code }}</strong><br /><span class="muted">{{ c.name }}</span></td>
              <td>{{ discountLabel(c.discountType) }}</td>
              <td>@if (c.discountType === 'Percentage') { {{ c.discountValue }}% } @else { {{ c.discountValue | money: c.currency }} }</td>
              <td>{{ c.timesRedeemed }}@if (c.usageLimit) { / {{ c.usageLimit }} }</td>
              <td>@if (c.isActive) { <span class="badge ok">{{ 'admin.promotions.active' | transloco }}</span> } @else { <span class="badge bad">{{ 'admin.promotions.off' | transloco }}</span> }</td>
              <td class="right">
                @if (c.isActive) { <button class="btn secondary sm" [csBusy]="busy()" [csBusy]="busy()" [disabled]="busy()" (click)="setActive(c, false)">{{ 'admin.c.deactivate' | transloco }}</button> }
                @else { <button class="btn secondary sm" [csBusy]="busy()" [csBusy]="busy()" [disabled]="busy()" (click)="setActive(c, true)">{{ 'admin.c.activate' | transloco }}</button> }
              </td>
            </tr>
          } @empty { <tr><td colspan="6" class="muted">{{ 'admin.promotions.none' | transloco }}</td></tr> }
        </tbody>
      </table>
      }
    </section>
  `,
  styles: [`section { margin-bottom: 1.25rem; }`],
})
export class PromotionsAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PromotionsAdminApi);
  private readonly i18n = inject(I18n);

  readonly discountTypes = computed(() => {
    this.i18n.lang(); // recompute when the language changes
    return [{ value: 0, label: this.i18n.t('admin.promotions.percentage') }, { value: 1, label: this.i18n.t('admin.promotions.fixed') }];
  });

  discountLabel(type: string): string {
    this.i18n.lang();
    return this.i18n.t(type === 'Percentage' ? 'admin.promotions.percentage' : 'admin.promotions.fixed');
  }
  readonly coupons = signal<Coupon[]>([]);
  readonly loaded = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly form = this.fb.nonNullable.group({
    code: ['', Validators.required],
    name: ['', Validators.required],
    discountType: [0, Validators.required],
    discountValue: [0, [Validators.required, Validators.min(0.01)]],
    currency: [STORE_CURRENCY, [Validators.required, Validators.maxLength(3)]],
    minimumSubtotal: [0, [Validators.min(0)]],
    usageLimit: [null as number | null],
    perCustomerLimit: [null as number | null],
  });

  ngOnInit(): void { this.load(); }
  private load(): void { this.api.list().subscribe({ next: (c) => { this.coupons.set(c); this.loaded.set(true); }, error: (e) => { this.error.set(this.i18n.problem(e)); this.loaded.set(true); } }); }
  private ok(m: string): void { this.message.set(m); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(this.i18n.problem(e)); this.busy.set(false); }

  create(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    const v = this.form.getRawValue();
    this.api.create({
      code: v.code, name: v.name, discountType: Number(v.discountType), discountValue: Number(v.discountValue),
      currency: v.currency.toUpperCase(), minimumSubtotal: Number(v.minimumSubtotal),
      usageLimit: v.usageLimit ? Number(v.usageLimit) : null,
      perCustomerLimit: v.perCustomerLimit ? Number(v.perCustomerLimit) : null,
    }).subscribe({ next: () => { this.ok(this.i18n.t('admin.promotions.created')); this.form.reset({ discountType: 0, discountValue: 0, currency: STORE_CURRENCY, minimumSubtotal: 0 }); }, error: (e) => this.fail(e) });
  }

  setActive(c: Coupon, active: boolean): void {
    this.busy.set(true);
    const op = active ? this.api.activate(c.id) : this.api.deactivate(c.id);
    op.subscribe({ next: () => this.ok(active ? this.i18n.t('admin.promotions.activated') : this.i18n.t('admin.promotions.deactivated')), error: (e) => this.fail(e) });
  }
}
