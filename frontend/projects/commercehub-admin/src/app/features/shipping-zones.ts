import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShippingAdminApi } from '@ch/api/shipping-admin.api';
import { PickupStore, ShippingZone } from '@ch/api/models';
import { HONDURAS_DEPARTMENTS } from '@ch/util/departments';
import { MoneyPipe } from '@ch/util/money.pipe';
import { problemMessage } from '@ch/util/problem';
import { STORE_CURRENCY } from '@ch/util/store';

/** Delivery zones (cost, lead time, cash-on-delivery coverage, departments) and the stores that offer pickup. */
@Component({
  selector: 'app-shipping-zones',
  imports: [ReactiveFormsModule, MoneyPipe],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>{{ editingZone() ? 'Edit zone' : 'New zone' }}</h2>
      <p class="muted">The delivery cost depends only on the destination zone. Each department belongs to one zone.</p>
      <form [formGroup]="zoneForm" (ngSubmit)="saveZone()">
        <div class="row">
          <div class="field" style="flex:1"><label>Code</label><input class="input" formControlName="code" [readonly]="!!editingZone()" /></div>
          <div class="field" style="flex:2"><label>Name</label><input class="input" formControlName="name" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>Cost</label><input class="input" type="number" step="0.01" min="0" formControlName="cost" /></div>
          <div class="field" style="flex:1"><label>Currency</label><input class="input" formControlName="currency" readonly title="The store sells in one currency" /></div>
          <div class="field" style="flex:1"><label>Min. days</label><input class="input" type="number" min="0" formControlName="estimatedDaysMin" /></div>
          <div class="field" style="flex:1"><label>Max. days</label><input class="input" type="number" min="0" formControlName="estimatedDaysMax" /></div>
        </div>
        <label class="check"><input type="checkbox" formControlName="cashOnDeliveryCovered" /> Cash on delivery available in this zone</label>
        <fieldset class="departments">
          <legend>Departments</legend>
          @for (d of departments; track d.code) {
            <label class="check"><input type="checkbox" [checked]="selected().has(d.code)" (change)="toggle(d.code, $event)" /> {{ d.name }}</label>
          }
        </fieldset>
        <div class="row">
          <button class="btn" [disabled]="zoneForm.invalid || selected().size === 0 || busy()">{{ editingZone() ? 'Save zone' : 'Create zone' }}</button>
          @if (editingZone()) { <button class="btn secondary" type="button" (click)="resetZone()">Cancel</button> }
        </div>
      </form>
    </section>

    <section class="card">
      <h2>Zones</h2>
      <table>
        <thead><tr><th>Zone</th><th>Cost</th><th>Days</th><th>Cash on delivery</th><th>Departments</th><th></th></tr></thead>
        <tbody>
          @for (z of zones(); track z.id) {
            <tr [class.muted]="!z.isActive">
              <td>{{ z.name }} <span class="muted">({{ z.code }})</span>@if (!z.isActive) { <span class="badge"> inactive</span> }</td>
              <td>{{ z.cost | money: z.currency }}</td>
              <td>{{ z.estimatedDaysMin }}–{{ z.estimatedDaysMax }}</td>
              <td>{{ z.cashOnDeliveryCovered ? 'Yes' : 'No' }}</td>
              <td class="small">{{ names(z) }}</td>
              <td class="right">
                <button class="btn secondary sm" (click)="editZone(z)">Edit</button>
                <button class="btn secondary sm" (click)="setZoneActive(z, !z.isActive)">{{ z.isActive ? 'Deactivate' : 'Activate' }}</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="6" class="muted">No zones yet: customers cannot choose home delivery until one exists.</td></tr> }
        </tbody>
      </table>
      @if (unassigned().length) { <p class="muted small">Not served by any active zone: {{ unassigned().join(', ') }}.</p> }
    </section>

    <section class="card">
      <h2>{{ editingStore() ? 'Edit pickup store' : 'New pickup store' }}</h2>
      <form [formGroup]="storeForm" (ngSubmit)="saveStore()">
        <div class="row">
          <div class="field" style="flex:2"><label>Name</label><input class="input" formControlName="name" /></div>
          <div class="field" style="flex:1"><label>Phone</label><input class="input" formControlName="phone" /></div>
        </div>
        <div class="field"><label>Address</label><input class="input" formControlName="line1" /></div>
        <div class="row">
          <div class="field" style="flex:1"><label>City</label><input class="input" formControlName="city" /></div>
          <div class="field" style="flex:1">
            <label>Department</label>
            <select formControlName="departmentCode">
              <option value="">Select…</option>
              @for (d of departments; track d.code) { <option [value]="d.code">{{ d.name }}</option> }
            </select>
          </div>
        </div>
        <div class="field"><label>Opening hours</label><input class="input" formControlName="hours" placeholder="Mon–Sat 9:00–18:00" /></div>
        <div class="row">
          <button class="btn" [disabled]="storeForm.invalid || busy()">{{ editingStore() ? 'Save store' : 'Create store' }}</button>
          @if (editingStore()) { <button class="btn secondary" type="button" (click)="resetStore()">Cancel</button> }
        </div>
      </form>
    </section>

    <section class="card">
      <h2>Pickup stores</h2>
      <table>
        <thead><tr><th>Store</th><th>Address</th><th>Hours</th><th></th></tr></thead>
        <tbody>
          @for (s of stores(); track s.id) {
            <tr [class.muted]="!s.isActive">
              <td>{{ s.name }}@if (!s.isActive) { <span class="badge"> disabled</span> }</td>
              <td class="small">{{ s.line1 }}, {{ s.city }}, {{ s.departmentName }}</td>
              <td class="small">{{ s.hours || '—' }}</td>
              <td class="right">
                <button class="btn secondary sm" (click)="editStore(s)">Edit</button>
                <button class="btn secondary sm" (click)="setStoreActive(s, !s.isActive)">{{ s.isActive ? 'Disable' : 'Enable' }}</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="4" class="muted">No pickup stores: customers can only choose home delivery.</td></tr> }
        </tbody>
      </table>
    </section>
  `,
  styles: [`
    section { margin-bottom: 1.25rem; }
    .check { display: flex; align-items: center; gap: 0.5rem; font-size: 0.9rem; }
    .departments { border: 1px solid var(--border); border-radius: var(--radius-sm); margin: 1rem 0; padding: 0.75rem 1rem; display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 0.4rem 1rem; }
    .departments legend { padding: 0 0.4rem; font-size: 0.82rem; color: var(--muted); }
    .small { font-size: 0.82rem; }
  `],
})
export class ShippingZones implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ShippingAdminApi);

  readonly departments = HONDURAS_DEPARTMENTS;
  readonly zones = signal<ShippingZone[]>([]);
  readonly stores = signal<PickupStore[]>([]);
  readonly selected = signal<ReadonlySet<string>>(new Set());
  readonly editingZone = signal<ShippingZone | null>(null);
  readonly editingStore = signal<PickupStore | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly zoneForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(40)]],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    cost: [0, [Validators.required, Validators.min(0)]],
    currency: [STORE_CURRENCY, [Validators.required]],
    estimatedDaysMin: [1, [Validators.required, Validators.min(0), Validators.max(60)]],
    estimatedDaysMax: [2, [Validators.required, Validators.min(0), Validators.max(60)]],
    cashOnDeliveryCovered: [false],
  });

  readonly storeForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    line1: ['', [Validators.required, Validators.maxLength(200)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    departmentCode: ['', [Validators.required]],
    phone: [''],
    hours: [''],
  });

  ngOnInit(): void { this.load(); }

  private load(): void {
    this.api.zones().subscribe({ next: (z) => this.zones.set(z), error: (e) => this.error.set(problemMessage(e)) });
    this.api.pickupStores().subscribe({ next: (s) => this.stores.set(s), error: (e) => this.error.set(problemMessage(e)) });
  }
  private ok(text: string): void { this.message.set(text); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(problemMessage(e)); this.message.set(null); this.busy.set(false); }

  names(zone: ShippingZone): string { return zone.departments.map((d) => d.name).join(', '); }

  /** Departments that no active zone serves: customers cannot have deliveries sent there. */
  unassigned(): string[] {
    const served = new Set(this.zones().filter((z) => z.isActive).flatMap((z) => z.departments.map((d) => d.code)));
    return this.departments.filter((d) => !served.has(d.code)).map((d) => d.name);
  }

  toggle(code: string, event: Event): void {
    const next = new Set(this.selected());
    if ((event.target as HTMLInputElement).checked) next.add(code); else next.delete(code);
    this.selected.set(next);
  }

  editZone(zone: ShippingZone): void {
    this.editingZone.set(zone);
    this.zoneForm.reset({
      code: zone.code, name: zone.name, cost: zone.cost, currency: zone.currency,
      estimatedDaysMin: zone.estimatedDaysMin, estimatedDaysMax: zone.estimatedDaysMax, cashOnDeliveryCovered: zone.cashOnDeliveryCovered,
    });
    this.selected.set(new Set(zone.departments.map((d) => d.code)));
  }

  resetZone(): void {
    this.editingZone.set(null);
    this.zoneForm.reset({ code: '', name: '', cost: 0, currency: STORE_CURRENCY, estimatedDaysMin: 1, estimatedDaysMax: 2, cashOnDeliveryCovered: false });
    this.selected.set(new Set());
  }

  saveZone(): void {
    if (this.zoneForm.invalid || this.selected().size === 0) return;
    this.busy.set(true);
    const v = this.zoneForm.getRawValue();
    const body = {
      name: v.name, cost: Number(v.cost), currency: v.currency.toUpperCase(), estimatedDaysMin: Number(v.estimatedDaysMin),
      estimatedDaysMax: Number(v.estimatedDaysMax), cashOnDeliveryCovered: v.cashOnDeliveryCovered, departmentCodes: [...this.selected()],
    };
    const editing = this.editingZone();
    const call = editing ? this.api.updateZone(editing.id, body) : this.api.createZone({ ...body, code: v.code });
    call.subscribe({ next: () => { this.ok(editing ? 'Zone saved.' : 'Zone created.'); this.resetZone(); }, error: (e) => this.fail(e) });
  }

  setZoneActive(zone: ShippingZone, active: boolean): void {
    this.busy.set(true);
    this.api.setZoneActive(zone.id, active).subscribe({ next: () => this.ok(active ? 'Zone activated.' : 'Zone deactivated.'), error: (e) => this.fail(e) });
  }

  editStore(store: PickupStore): void {
    this.editingStore.set(store);
    this.storeForm.reset({
      name: store.name, line1: store.line1, city: store.city, departmentCode: store.departmentCode,
      phone: store.phone ?? '', hours: store.hours ?? '',
    });
  }

  resetStore(): void {
    this.editingStore.set(null);
    this.storeForm.reset({ name: '', line1: '', city: '', departmentCode: '', phone: '', hours: '' });
  }

  saveStore(): void {
    if (this.storeForm.invalid) return;
    this.busy.set(true);
    const v = this.storeForm.getRawValue();
    const body = { name: v.name, line1: v.line1, city: v.city, departmentCode: v.departmentCode, phone: v.phone || null, hours: v.hours || null };
    const editing = this.editingStore();
    const call = editing ? this.api.updatePickupStore(editing.id, body) : this.api.createPickupStore(body);
    call.subscribe({ next: () => { this.ok(editing ? 'Store saved.' : 'Store created.'); this.resetStore(); }, error: (e) => this.fail(e) });
  }

  setStoreActive(store: PickupStore, active: boolean): void {
    this.busy.set(true);
    this.api.setPickupStoreActive(store.id, active).subscribe({ next: () => this.ok(active ? 'Store enabled.' : 'Store disabled.'), error: (e) => this.fail(e) });
  }
}
