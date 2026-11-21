import { STORE_CURRENCY } from '@cs/util/store';
import { BusyButton } from '@cs/ui/busy-button';
import { TableSkeleton } from '../ui/skeleton';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShippingApi } from '@cs/api/shipping.api';
import { ShippingAdminApi } from '@cs/api/shipping-admin.api';
import { ShippingMethod } from '@cs/api/models';
import { MoneyPipe } from '@cs/util/money.pipe';
import { ShippingZones } from './shipping-zones';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';

@Component({
  selector: 'app-shipping-admin',
  imports: [ReactiveFormsModule, MoneyPipe, ShippingZones, TranslocoPipe, BusyButton, TableSkeleton],
  template: `
    <h1>{{ 'admin.nav.shipping' | transloco }}</h1>
    <app-shipping-zones />
    <h2 class="section-title">{{ 'admin.shipping.methods' | transloco }}</h2>
    <p class="muted">{{ 'admin.shipping.methodsSub' | transloco }}</p>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>{{ 'admin.shipping.newMethod' | transloco }}</h2>
      <form [formGroup]="form" (ngSubmit)="create()">
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.code' | transloco }}</label><input class="input" formControlName="code" /></div>
          <div class="field" style="flex:2"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.cost' | transloco }}</label><input class="input" type="number" step="0.01" formControlName="cost" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.c.currency' | transloco }}</label><input class="input" maxlength="3" formControlName="currency" readonly [title]="'admin.c.oneCurrency' | transloco" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.shipping.estDays' | transloco }}</label><input class="input" type="number" formControlName="estimatedDays" /></div>
        </div>
        <button class="btn" [csBusy]="busy()" [disabled]="form.invalid || busy()">{{ 'admin.shipping.createMethod' | transloco }}</button>
      </form>
    </section>

    <section class="card">
      <h2>{{ 'admin.shipping.activeMethods' | transloco }}</h2>
      <p class="muted">{{ 'admin.shipping.activeSub' | transloco }}</p>
      @if (!loaded()) { <app-table-skeleton [rows]="3" [cols]="5" /> } @else {
      <table>
        <thead><tr><th>{{ 'admin.c.code' | transloco }}</th><th>{{ 'admin.c.name' | transloco }}</th><th>{{ 'admin.c.cost' | transloco }}</th><th>{{ 'admin.shipping.est' | transloco }}</th><th></th></tr></thead>
        <tbody>
          @for (m of methods(); track m.id) {
            <tr>
              <td>{{ m.code }}</td><td>{{ m.name }}</td><td>{{ m.cost | money: m.currency }}</td><td>{{ m.estimatedDays }}d</td>
              <td class="right"><button class="btn secondary sm" [csBusy]="busy()" [csBusy]="busy()" [disabled]="busy()" (click)="deactivate(m)">{{ 'admin.c.deactivate' | transloco }}</button></td>
            </tr>
          } @empty { <tr><td colspan="5" class="muted">{{ 'admin.shipping.noMethods' | transloco }}</td></tr> }
        </tbody>
      </table>
      }
    </section>
  `,
  styles: [`section { margin-bottom: 1.25rem; }`],
})
export class ShippingAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly publicApi = inject(ShippingApi);
  private readonly api = inject(ShippingAdminApi);
  private readonly i18n = inject(I18n);

  readonly methods = signal<ShippingMethod[]>([]);
  readonly loaded = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly form = this.fb.nonNullable.group({
    code: ['', Validators.required],
    name: ['', Validators.required],
    cost: [0, [Validators.required, Validators.min(0)]],
    currency: [STORE_CURRENCY, [Validators.required, Validators.maxLength(3)]],
    estimatedDays: [3, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void { this.load(); }
  private load(): void { this.publicApi.methods().subscribe({ next: (m) => { this.methods.set(m); this.loaded.set(true); }, error: (e) => { this.error.set(this.i18n.problem(e)); this.loaded.set(true); } }); }
  private ok(m: string): void { this.message.set(m); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(this.i18n.problem(e)); this.busy.set(false); }

  create(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    const v = this.form.getRawValue();
    this.api.createMethod({ code: v.code, name: v.name, cost: Number(v.cost), currency: v.currency.toUpperCase(), estimatedDays: Number(v.estimatedDays) })
      .subscribe({ next: () => { this.ok(this.i18n.t('admin.shipping.methodCreated')); this.form.reset({ cost: 0, currency: STORE_CURRENCY, estimatedDays: 3 }); }, error: (e) => this.fail(e) });
  }
  deactivate(m: ShippingMethod): void {
    this.busy.set(true);
    this.api.deactivate(m.id).subscribe({ next: () => this.ok(this.i18n.t('admin.shipping.methodDeactivated')), error: (e) => this.fail(e) });
  }
}
