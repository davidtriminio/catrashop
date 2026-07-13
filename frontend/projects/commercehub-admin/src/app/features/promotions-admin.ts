import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PromotionsAdminApi } from '@ch/api/promotions-admin.api';
import { Coupon } from '@ch/api/models';
import { MoneyPipe } from '@ch/util/money.pipe';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-promotions-admin',
  imports: [ReactiveFormsModule, MoneyPipe],
  template: `
    <h1>Promotions</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>New coupon</h2>
      <form [formGroup]="form" (ngSubmit)="create()">
        <div class="row">
          <div class="field" style="flex:1"><label>Code</label><input class="input" formControlName="code" /></div>
          <div class="field" style="flex:2"><label>Name</label><input class="input" formControlName="name" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>Type</label>
            <select formControlName="discountType"><option [value]="0">Percentage</option><option [value]="1">Fixed amount</option></select>
          </div>
          <div class="field" style="flex:1"><label>Value</label><input class="input" type="number" step="0.01" formControlName="discountValue" /></div>
          <div class="field" style="flex:1"><label>Currency</label><input class="input" maxlength="3" formControlName="currency" /></div>
          <div class="field" style="flex:1"><label>Min subtotal</label><input class="input" type="number" step="0.01" formControlName="minimumSubtotal" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>Usage limit</label><input class="input" type="number" formControlName="usageLimit" placeholder="∞" /></div>
          <div class="field" style="flex:1"><label>Per customer</label><input class="input" type="number" formControlName="perCustomerLimit" placeholder="∞" /></div>
        </div>
        <button class="btn" [disabled]="form.invalid || busy()">Create coupon</button>
      </form>
    </section>

    <section class="card">
      <h2>Coupons</h2>
      <table>
        <thead><tr><th>Code</th><th>Type</th><th>Value</th><th>Redeemed</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (c of coupons(); track c.id) {
            <tr>
              <td><strong>{{ c.code }}</strong><br /><span class="muted">{{ c.name }}</span></td>
              <td>{{ c.discountType }}</td>
              <td>@if (c.discountType === 'Percentage') { {{ c.discountValue }}% } @else { {{ c.discountValue | money: (c.currency || 'USD') }} }</td>
              <td>{{ c.timesRedeemed }}@if (c.usageLimit) { / {{ c.usageLimit }} }</td>
              <td>@if (c.isActive) { <span class="badge ok">Active</span> } @else { <span class="badge bad">Off</span> }</td>
              <td class="right">
                @if (c.isActive) { <button class="btn secondary sm" (click)="setActive(c, false)">Deactivate</button> }
                @else { <button class="btn secondary sm" (click)="setActive(c, true)">Activate</button> }
              </td>
            </tr>
          } @empty { <tr><td colspan="6" class="muted">No coupons yet.</td></tr> }
        </tbody>
      </table>
    </section>
  `,
  styles: [`section { margin-bottom: 1.25rem; }`],
})
export class PromotionsAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(PromotionsAdminApi);

  readonly coupons = signal<Coupon[]>([]);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly form = this.fb.nonNullable.group({
    code: ['', Validators.required],
    name: ['', Validators.required],
    discountType: [0, Validators.required],
    discountValue: [0, [Validators.required, Validators.min(0.01)]],
    currency: ['USD', [Validators.required, Validators.maxLength(3)]],
    minimumSubtotal: [0, [Validators.min(0)]],
    usageLimit: [null as number | null],
    perCustomerLimit: [null as number | null],
  });

  ngOnInit(): void { this.load(); }
  private load(): void { this.api.list().subscribe({ next: (c) => this.coupons.set(c), error: (e) => this.error.set(problemMessage(e)) }); }
  private ok(m: string): void { this.message.set(m); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(problemMessage(e)); this.busy.set(false); }

  create(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    const v = this.form.getRawValue();
    this.api.create({
      code: v.code, name: v.name, discountType: Number(v.discountType), discountValue: Number(v.discountValue),
      currency: v.currency.toUpperCase(), minimumSubtotal: Number(v.minimumSubtotal),
      usageLimit: v.usageLimit ? Number(v.usageLimit) : null,
      perCustomerLimit: v.perCustomerLimit ? Number(v.perCustomerLimit) : null,
    }).subscribe({ next: () => { this.ok('Coupon created.'); this.form.reset({ discountType: 0, discountValue: 0, currency: 'USD', minimumSubtotal: 0 }); }, error: (e) => this.fail(e) });
  }

  setActive(c: Coupon, active: boolean): void {
    this.busy.set(true);
    const op = active ? this.api.activate(c.id) : this.api.deactivate(c.id);
    op.subscribe({ next: () => this.ok(active ? 'Activated.' : 'Deactivated.'), error: (e) => this.fail(e) });
  }
}
