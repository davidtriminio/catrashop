import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShippingApi } from '@ch/api/shipping.api';
import { ShippingAdminApi } from '@ch/api/shipping-admin.api';
import { ShippingMethod } from '@ch/api/models';
import { MoneyPipe } from '@ch/util/money.pipe';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-shipping-admin',
  imports: [ReactiveFormsModule, MoneyPipe],
  template: `
    <h1>Shipping methods</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>New method</h2>
      <form [formGroup]="form" (ngSubmit)="create()">
        <div class="row">
          <div class="field" style="flex:1"><label>Code</label><input class="input" formControlName="code" /></div>
          <div class="field" style="flex:2"><label>Name</label><input class="input" formControlName="name" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>Cost</label><input class="input" type="number" step="0.01" formControlName="cost" /></div>
          <div class="field" style="flex:1"><label>Currency</label><input class="input" maxlength="3" formControlName="currency" /></div>
          <div class="field" style="flex:1"><label>Est. days</label><input class="input" type="number" formControlName="estimatedDays" /></div>
        </div>
        <button class="btn" [disabled]="form.invalid || busy()">Create method</button>
      </form>
    </section>

    <section class="card">
      <h2>Active methods</h2>
      <p class="muted">The list shows methods offered to customers (active).</p>
      <table>
        <thead><tr><th>Code</th><th>Name</th><th>Cost</th><th>Est.</th><th></th></tr></thead>
        <tbody>
          @for (m of methods(); track m.id) {
            <tr>
              <td>{{ m.code }}</td><td>{{ m.name }}</td><td>{{ m.cost | money: m.currency }}</td><td>{{ m.estimatedDays }}d</td>
              <td class="right"><button class="btn secondary sm" (click)="deactivate(m)">Deactivate</button></td>
            </tr>
          } @empty { <tr><td colspan="5" class="muted">No active methods.</td></tr> }
        </tbody>
      </table>
    </section>
  `,
  styles: [`section { margin-bottom: 1.25rem; }`],
})
export class ShippingAdmin implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly publicApi = inject(ShippingApi);
  private readonly api = inject(ShippingAdminApi);

  readonly methods = signal<ShippingMethod[]>([]);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly form = this.fb.nonNullable.group({
    code: ['', Validators.required],
    name: ['', Validators.required],
    cost: [0, [Validators.required, Validators.min(0)]],
    currency: ['USD', [Validators.required, Validators.maxLength(3)]],
    estimatedDays: [3, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void { this.load(); }
  private load(): void { this.publicApi.methods().subscribe({ next: (m) => this.methods.set(m), error: (e) => this.error.set(problemMessage(e)) }); }
  private ok(m: string): void { this.message.set(m); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(problemMessage(e)); this.busy.set(false); }

  create(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    const v = this.form.getRawValue();
    this.api.createMethod({ code: v.code, name: v.name, cost: Number(v.cost), currency: v.currency.toUpperCase(), estimatedDays: Number(v.estimatedDays) })
      .subscribe({ next: () => { this.ok('Method created.'); this.form.reset({ cost: 0, currency: 'USD', estimatedDays: 3 }); }, error: (e) => this.fail(e) });
  }
  deactivate(m: ShippingMethod): void {
    this.busy.set(true);
    this.api.deactivate(m.id).subscribe({ next: () => this.ok('Method deactivated.'), error: (e) => this.fail(e) });
  }
}
