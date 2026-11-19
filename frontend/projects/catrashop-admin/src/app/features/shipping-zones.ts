import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShippingAdminApi } from '@cs/api/shipping-admin.api';
import { PickupStore, ShippingZone } from '@cs/api/models';
import { HONDURAS_DEPARTMENTS } from '@cs/util/departments';
import { MoneyPipe } from '@cs/util/money.pipe';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { STORE_CURRENCY } from '@cs/util/store';
import { Select } from '../ui/select';

/** Delivery zones (cost, lead time, cash-on-delivery coverage, departments) and the stores that offer pickup. */
@Component({
  selector: 'app-shipping-zones',
  imports: [ReactiveFormsModule, MoneyPipe, Select, TranslocoPipe],
  template: `
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>{{ (editingZone() ? 'admin.zones.editZone' : 'admin.zones.newZone') | transloco }}</h2>
      <p class="muted">{{ 'admin.zones.sub' | transloco }}</p>
      <form [formGroup]="zoneForm" (ngSubmit)="saveZone()">
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.code' | transloco }}</label><input class="input" formControlName="code" [readonly]="!!editingZone()" /></div>
          <div class="field" style="flex:2"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
        </div>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.cost' | transloco }}</label><input class="input" type="number" step="0.01" min="0" formControlName="cost" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.c.currency' | transloco }}</label><input class="input" formControlName="currency" readonly [title]="'admin.c.oneCurrency' | transloco" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.zones.minDays' | transloco }}</label><input class="input" type="number" min="0" formControlName="estimatedDaysMin" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.zones.maxDays' | transloco }}</label><input class="input" type="number" min="0" formControlName="estimatedDaysMax" /></div>
        </div>
        <label class="check"><input type="checkbox" formControlName="cashOnDeliveryCovered" /> {{ 'admin.zones.cod' | transloco }}</label>
        <fieldset class="departments">
          <legend>{{ 'admin.zones.departments' | transloco }}</legend>
          @for (d of departments; track d.code) {
            <label class="check"><input type="checkbox" [checked]="selected().has(d.code)" (change)="toggle(d.code, $event)" /> {{ d.name }}</label>
          }
        </fieldset>
        <div class="row">
          <button class="btn" [disabled]="zoneForm.invalid || selected().size === 0 || busy()">{{ (editingZone() ? 'admin.zones.saveZone' : 'admin.zones.createZone') | transloco }}</button>
          @if (editingZone()) { <button class="btn secondary" type="button" (click)="resetZone()">{{ 'admin.c.cancel' | transloco }}</button> }
        </div>
      </form>
    </section>

    <section class="card">
      <h2>{{ 'admin.zones.zones' | transloco }}</h2>
      <table>
        <thead><tr><th>{{ 'admin.zones.zone' | transloco }}</th><th>{{ 'admin.c.cost' | transloco }}</th><th>{{ 'admin.zones.days' | transloco }}</th><th>{{ 'admin.zones.codCol' | transloco }}</th><th>{{ 'admin.zones.departments' | transloco }}</th><th></th></tr></thead>
        <tbody>
          @for (z of zones(); track z.id) {
            <tr [class.muted]="!z.isActive">
              <td>{{ z.name }} <span class="muted">({{ z.code }})</span>@if (!z.isActive) { <span class="badge">{{ 'admin.zones.inactive' | transloco }}</span> }</td>
              <td>{{ z.cost | money: z.currency }}</td>
              <td>{{ z.estimatedDaysMin }}–{{ z.estimatedDaysMax }}</td>
              <td>{{ (z.cashOnDeliveryCovered ? 'admin.zones.yes' : 'admin.zones.no') | transloco }}</td>
              <td class="small">{{ names(z) }}</td>
              <td class="right">
                <button class="btn secondary sm" (click)="editZone(z)">{{ 'admin.c.edit' | transloco }}</button>
                <button class="btn secondary sm" (click)="setZoneActive(z, !z.isActive)">{{ (z.isActive ? 'admin.c.deactivate' : 'admin.c.activate') | transloco }}</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="6" class="muted">{{ 'admin.zones.none' | transloco }}</td></tr> }
        </tbody>
      </table>
      @if (unassigned().length) { <p class="muted small">{{ 'admin.zones.unassigned' | transloco }} {{ unassigned().join(', ') }}.</p> }
    </section>

    <section class="card">
      <h2>{{ (editingStore() ? 'admin.zones.editStore' : 'admin.zones.newStore') | transloco }}</h2>
      <form [formGroup]="storeForm" (ngSubmit)="saveStore()">
        <div class="row">
          <div class="field" style="flex:2"><label>{{ 'admin.c.name' | transloco }}</label><input class="input" formControlName="name" /></div>
          <div class="field" style="flex:1"><label>{{ 'admin.c.phone' | transloco }}</label><input class="input" formControlName="phone" /></div>
        </div>
        <div class="field"><label>{{ 'admin.c.address' | transloco }}</label><input class="input" formControlName="line1" /></div>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.city' | transloco }}</label><input class="input" formControlName="city" /></div>
          <div class="field" style="flex:1">
            <label>{{ 'admin.c.department' | transloco }}</label>
            <cs-select formControlName="departmentCode" [options]="departmentOptions" [placeholder]="'admin.c.select' | transloco" />
          </div>
        </div>
        <div class="field"><label>{{ 'admin.c.hours' | transloco }}</label><input class="input" formControlName="hours" [placeholder]="'admin.zones.hoursExample' | transloco" /></div>
        <div class="row">
          <button class="btn" [disabled]="storeForm.invalid || busy()">{{ (editingStore() ? 'admin.zones.saveStore' : 'admin.zones.createStore') | transloco }}</button>
          @if (editingStore()) { <button class="btn secondary" type="button" (click)="resetStore()">{{ 'admin.c.cancel' | transloco }}</button> }
        </div>
      </form>
    </section>

    <section class="card">
      <h2>{{ 'admin.zones.stores' | transloco }}</h2>
      <table>
        <thead><tr><th>{{ 'admin.zones.store' | transloco }}</th><th>{{ 'admin.c.address' | transloco }}</th><th>{{ 'admin.zones.hoursCol' | transloco }}</th><th></th></tr></thead>
        <tbody>
          @for (s of stores(); track s.id) {
            <tr [class.muted]="!s.isActive">
              <td>{{ s.name }}@if (!s.isActive) { <span class="badge">{{ 'admin.zones.disabled' | transloco }}</span> }</td>
              <td class="small">{{ s.line1 }}, {{ s.city }}, {{ s.departmentName }}</td>
              <td class="small">{{ s.hours || '—' }}</td>
              <td class="right">
                <button class="btn secondary sm" (click)="editStore(s)">{{ 'admin.c.edit' | transloco }}</button>
                <button class="btn secondary sm" (click)="setStoreActive(s, !s.isActive)">{{ (s.isActive ? 'admin.zones.disable' : 'admin.zones.enable') | transloco }}</button>
              </td>
            </tr>
          } @empty { <tr><td colspan="4" class="muted">{{ 'admin.zones.noStores' | transloco }}</td></tr> }
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
  private readonly i18n = inject(I18n);

  readonly departments = HONDURAS_DEPARTMENTS;
  readonly departmentOptions = HONDURAS_DEPARTMENTS.map((d) => ({ value: d.code, label: d.name }));
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
    this.api.zones().subscribe({ next: (z) => this.zones.set(z), error: (e) => this.error.set(this.i18n.problem(e)) });
    this.api.pickupStores().subscribe({ next: (s) => this.stores.set(s), error: (e) => this.error.set(this.i18n.problem(e)) });
  }
  private ok(text: string): void { this.message.set(text); this.error.set(null); this.busy.set(false); this.load(); }
  private fail(e: unknown): void { this.error.set(this.i18n.problem(e)); this.message.set(null); this.busy.set(false); }

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
    call.subscribe({ next: () => { this.ok(editing ? this.i18n.t('admin.zones.saved') : this.i18n.t('admin.zones.created')); this.resetZone(); }, error: (e) => this.fail(e) });
  }

  setZoneActive(zone: ShippingZone, active: boolean): void {
    this.busy.set(true);
    this.api.setZoneActive(zone.id, active).subscribe({ next: () => this.ok(active ? this.i18n.t('admin.zones.activated') : this.i18n.t('admin.zones.deactivated')), error: (e) => this.fail(e) });
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
    call.subscribe({ next: () => { this.ok(editing ? this.i18n.t('admin.zones.storeSaved') : this.i18n.t('admin.zones.storeCreated')); this.resetStore(); }, error: (e) => this.fail(e) });
  }

  setStoreActive(store: PickupStore, active: boolean): void {
    this.busy.set(true);
    this.api.setPickupStoreActive(store.id, active).subscribe({ next: () => this.ok(active ? this.i18n.t('admin.zones.storeEnabled') : this.i18n.t('admin.zones.storeDisabled')), error: (e) => this.fail(e) });
  }
}
