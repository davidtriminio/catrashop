import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { CustomersApi } from '@ch/api/customers.api';
import { Address, CustomerProfile, WishlistItem } from '@ch/api/models';
import { I18n } from '@ch/i18n/i18n';
import { LocalDatePipe } from '@ch/util/local-date.pipe';

@Component({
  selector: 'app-account-page',
  imports: [ReactiveFormsModule, LocalDatePipe, TranslocoPipe],
  template: `
    <h1>{{ 'account.title' | transloco }}</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>{{ 'account.profile' | transloco }}</h2>
      @if (profile()) {
        <form [formGroup]="profileForm" (ngSubmit)="saveProfile()">
          <p class="muted">{{ profile()?.email }}</p>
          <div class="row">
            <div class="field" style="flex:1"><label>{{ 'auth.firstName' | transloco }}</label><input class="input" formControlName="firstName" /></div>
            <div class="field" style="flex:1"><label>{{ 'auth.lastName' | transloco }}</label><input class="input" formControlName="lastName" /></div>
          </div>
          <div class="field" style="max-width:260px"><label>{{ 'account.phone' | transloco }}</label><input class="input" formControlName="phone" /></div>
          <button class="btn" type="submit" [disabled]="profileForm.invalid || busy()">{{ 'account.saveProfile' | transloco }}</button>
        </form>
      }
    </section>

    <section class="card">
      <h2>{{ 'account.addresses' | transloco }}</h2>
      @for (a of addresses(); track a.id) {
        <div class="addr">
          <div>
            <strong>{{ a.label }}</strong>
            @if (a.isDefaultShipping) { <span class="badge ok">{{ 'account.defaultShipping' | transloco }}</span> }
            @if (a.isDefaultBilling) { <span class="badge">{{ 'account.defaultBilling' | transloco }}</span> }
            <div class="muted">{{ a.recipientName }} — {{ a.line1 }}, {{ a.city }} {{ a.postalCode }}, {{ a.country }}</div>
          </div>
          <div class="row">
            @if (!a.isDefaultShipping) { <button class="btn secondary sm" (click)="defaultShipping(a)">{{ 'account.setShipping' | transloco }}</button> }
            @if (!a.isDefaultBilling) { <button class="btn secondary sm" (click)="defaultBilling(a)">{{ 'account.setBilling' | transloco }}</button> }
            <button class="btn danger sm" (click)="removeAddress(a)">{{ 'account.delete' | transloco }}</button>
          </div>
        </div>
      } @empty { <p class="muted">{{ 'account.noAddresses' | transloco }}</p> }

      <details class="new">
        <summary>{{ 'account.addAddress' | transloco }}</summary>
        <form [formGroup]="addressForm" (ngSubmit)="addAddress()">
          <div class="row">
            <div class="field" style="flex:1"><label>{{ 'account.label' | transloco }}</label><input class="input" formControlName="label" [placeholder]="'account.labelPlaceholder' | transloco" /></div>
            <div class="field" style="flex:2"><label>{{ 'account.recipient' | transloco }}</label><input class="input" formControlName="recipientName" /></div>
          </div>
          <div class="field"><label>{{ 'account.line1' | transloco }}</label><input class="input" formControlName="line1" /></div>
          <div class="field"><label>{{ 'account.line2' | transloco }}</label><input class="input" formControlName="line2" /></div>
          <div class="row">
            <div class="field" style="flex:2"><label>{{ 'account.city' | transloco }}</label><input class="input" formControlName="city" /></div>
            <div class="field" style="flex:1"><label>{{ 'account.region' | transloco }}</label><input class="input" formControlName="region" /></div>
          </div>
          <div class="row">
            <div class="field" style="flex:1"><label>{{ 'account.postalCode' | transloco }}</label><input class="input" formControlName="postalCode" /></div>
            <div class="field" style="flex:1"><label>{{ 'account.country' | transloco }}</label><input class="input" maxlength="2" formControlName="country" /></div>
            <div class="field" style="flex:1"><label>{{ 'account.phone' | transloco }}</label><input class="input" formControlName="phone" /></div>
          </div>
          <button class="btn" type="submit" [disabled]="addressForm.invalid || busy()">{{ 'account.addAddressSubmit' | transloco }}</button>
        </form>
      </details>
    </section>

    <section class="card">
      <h2>{{ 'account.wishlist' | transloco }}</h2>
      @for (w of wishlist(); track w.productId) {
        <div class="addr">
          <div><span class="muted">{{ 'account.product' | transloco }}</span> {{ w.productId }}<div class="muted">{{ 'account.added' | transloco: { date: (w.addedAtUtc | localDate: 'date') } }}</div></div>
          <button class="btn secondary sm" (click)="removeWishlist(w)">{{ 'cart.remove' | transloco }}</button>
        </div>
      } @empty { <p class="muted">{{ 'account.wishlistEmpty' | transloco }}</p> }
    </section>
  `,
  styles: [`
    section { margin-bottom: 1.5rem; }
    .addr { display: flex; justify-content: space-between; align-items: center; gap: 1rem; padding: 0.6rem 0; border-bottom: 1px solid var(--border); }
    .new { margin-top: 1rem; }
    .new summary { cursor: pointer; font-weight: 600; color: var(--primary); }
  `],
})
export class AccountPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(CustomersApi);
  private readonly i18n = inject(I18n);

  readonly profile = signal<CustomerProfile | null>(null);
  readonly addresses = signal<Address[]>([]);
  readonly wishlist = signal<WishlistItem[]>([]);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  readonly profileForm = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    phone: [''],
  });

  readonly addressForm = this.fb.nonNullable.group({
    label: ['', [Validators.required]],
    recipientName: ['', [Validators.required]],
    line1: ['', [Validators.required]],
    line2: [''],
    city: ['', [Validators.required]],
    region: [''],
    postalCode: ['', [Validators.required]],
    country: ['', [Validators.required, Validators.maxLength(2)]],
    phone: [''],
  });

  ngOnInit(): void {
    this.api.profile().subscribe({
      next: (p) => { this.profile.set(p); this.profileForm.patchValue({ firstName: p.firstName, lastName: p.lastName, phone: p.phone ?? '' }); },
      error: (err) => this.error.set(this.i18n.problem(err)),
    });
    this.loadAddresses();
    this.api.wishlist().subscribe({ next: (w) => this.wishlist.set(w), error: () => {} });
  }

  private loadAddresses(): void {
    this.api.addresses().subscribe({ next: (a) => this.addresses.set(a), error: () => {} });
  }

  private ok(msg: string): void { this.message.set(msg); this.error.set(null); this.busy.set(false); }
  private fail(err: unknown): void { this.error.set(this.i18n.problem(err)); this.busy.set(false); }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.busy.set(true);
    this.api.updateProfile(this.profileForm.getRawValue()).subscribe({
      next: (p) => { this.profile.set(p); this.ok(this.i18n.t('account.messages.profileUpdated')); },
      error: (err) => this.fail(err),
    });
  }

  addAddress(): void {
    if (this.addressForm.invalid) return;
    this.busy.set(true);
    const v = this.addressForm.getRawValue();
    this.api.addAddress({ ...v, country: v.country.toUpperCase() }).subscribe({
      next: () => { this.ok(this.i18n.t('account.messages.addressAdded')); this.addressForm.reset(); this.loadAddresses(); },
      error: (err) => this.fail(err),
    });
  }

  removeAddress(a: Address): void {
    this.busy.set(true);
    this.api.removeAddress(a.id).subscribe({ next: () => { this.ok(this.i18n.t('account.messages.addressRemoved')); this.loadAddresses(); }, error: (err) => this.fail(err) });
  }
  defaultShipping(a: Address): void {
    this.busy.set(true);
    this.api.setDefaultShipping(a.id).subscribe({ next: () => { this.ok(this.i18n.t('account.messages.shippingUpdated')); this.loadAddresses(); }, error: (err) => this.fail(err) });
  }
  defaultBilling(a: Address): void {
    this.busy.set(true);
    this.api.setDefaultBilling(a.id).subscribe({ next: () => { this.ok(this.i18n.t('account.messages.billingUpdated')); this.loadAddresses(); }, error: (err) => this.fail(err) });
  }

  removeWishlist(w: WishlistItem): void {
    this.busy.set(true);
    this.api.removeWishlist(w.productId).subscribe({
      next: () => { this.ok(this.i18n.t('account.messages.wishlistRemoved')); this.wishlist.update((items) => items.filter((i) => i.productId !== w.productId)); },
      error: (err) => this.fail(err),
    });
  }
}
