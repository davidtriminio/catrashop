import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CustomersApi } from '@ch/api/customers.api';
import { Address, CustomerProfile, WishlistItem } from '@ch/api/models';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-account-page',
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <h1>My account</h1>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card">
      <h2>Profile</h2>
      @if (profile()) {
        <form [formGroup]="profileForm" (ngSubmit)="saveProfile()">
          <p class="muted">{{ profile()?.email }}</p>
          <div class="row">
            <div class="field" style="flex:1"><label>First name</label><input class="input" formControlName="firstName" /></div>
            <div class="field" style="flex:1"><label>Last name</label><input class="input" formControlName="lastName" /></div>
          </div>
          <div class="field" style="max-width:260px"><label>Phone</label><input class="input" formControlName="phone" /></div>
          <button class="btn" type="submit" [disabled]="profileForm.invalid || busy()">Save profile</button>
        </form>
      }
    </section>

    <section class="card">
      <h2>Addresses</h2>
      @for (a of addresses(); track a.id) {
        <div class="addr">
          <div>
            <strong>{{ a.label }}</strong>
            @if (a.isDefaultShipping) { <span class="badge ok">Default shipping</span> }
            @if (a.isDefaultBilling) { <span class="badge">Default billing</span> }
            <div class="muted">{{ a.recipientName }} — {{ a.line1 }}, {{ a.city }} {{ a.postalCode }}, {{ a.country }}</div>
          </div>
          <div class="row">
            @if (!a.isDefaultShipping) { <button class="btn secondary sm" (click)="defaultShipping(a)">Set shipping</button> }
            @if (!a.isDefaultBilling) { <button class="btn secondary sm" (click)="defaultBilling(a)">Set billing</button> }
            <button class="btn danger sm" (click)="removeAddress(a)">Delete</button>
          </div>
        </div>
      } @empty { <p class="muted">No saved addresses.</p> }

      <details class="new">
        <summary>Add an address</summary>
        <form [formGroup]="addressForm" (ngSubmit)="addAddress()">
          <div class="row">
            <div class="field" style="flex:1"><label>Label</label><input class="input" formControlName="label" placeholder="Home" /></div>
            <div class="field" style="flex:2"><label>Recipient</label><input class="input" formControlName="recipientName" /></div>
          </div>
          <div class="field"><label>Line 1</label><input class="input" formControlName="line1" /></div>
          <div class="field"><label>Line 2</label><input class="input" formControlName="line2" /></div>
          <div class="row">
            <div class="field" style="flex:2"><label>City</label><input class="input" formControlName="city" /></div>
            <div class="field" style="flex:1"><label>Region</label><input class="input" formControlName="region" /></div>
          </div>
          <div class="row">
            <div class="field" style="flex:1"><label>Postal code</label><input class="input" formControlName="postalCode" /></div>
            <div class="field" style="flex:1"><label>Country (ISO-2)</label><input class="input" maxlength="2" formControlName="country" /></div>
            <div class="field" style="flex:1"><label>Phone</label><input class="input" formControlName="phone" /></div>
          </div>
          <button class="btn" type="submit" [disabled]="addressForm.invalid || busy()">Add address</button>
        </form>
      </details>
    </section>

    <section class="card">
      <h2>Wishlist</h2>
      @for (w of wishlist(); track w.productId) {
        <div class="addr">
          <div><span class="muted">Product</span> {{ w.productId }}<div class="muted">Added {{ w.addedAtUtc | date: 'mediumDate' }}</div></div>
          <button class="btn secondary sm" (click)="removeWishlist(w)">Remove</button>
        </div>
      } @empty { <p class="muted">Your wishlist is empty.</p> }
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
      error: (err) => this.error.set(problemMessage(err)),
    });
    this.loadAddresses();
    this.api.wishlist().subscribe({ next: (w) => this.wishlist.set(w), error: () => {} });
  }

  private loadAddresses(): void {
    this.api.addresses().subscribe({ next: (a) => this.addresses.set(a), error: () => {} });
  }

  private ok(msg: string): void { this.message.set(msg); this.error.set(null); this.busy.set(false); }
  private fail(err: unknown): void { this.error.set(problemMessage(err)); this.busy.set(false); }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.busy.set(true);
    this.api.updateProfile(this.profileForm.getRawValue()).subscribe({
      next: (p) => { this.profile.set(p); this.ok('Profile updated.'); },
      error: (err) => this.fail(err),
    });
  }

  addAddress(): void {
    if (this.addressForm.invalid) return;
    this.busy.set(true);
    const v = this.addressForm.getRawValue();
    this.api.addAddress({ ...v, country: v.country.toUpperCase() }).subscribe({
      next: () => { this.ok('Address added.'); this.addressForm.reset(); this.loadAddresses(); },
      error: (err) => this.fail(err),
    });
  }

  removeAddress(a: Address): void {
    this.busy.set(true);
    this.api.removeAddress(a.id).subscribe({ next: () => { this.ok('Address removed.'); this.loadAddresses(); }, error: (err) => this.fail(err) });
  }
  defaultShipping(a: Address): void {
    this.busy.set(true);
    this.api.setDefaultShipping(a.id).subscribe({ next: () => { this.ok('Default shipping updated.'); this.loadAddresses(); }, error: (err) => this.fail(err) });
  }
  defaultBilling(a: Address): void {
    this.busy.set(true);
    this.api.setDefaultBilling(a.id).subscribe({ next: () => { this.ok('Default billing updated.'); this.loadAddresses(); }, error: (err) => this.fail(err) });
  }

  removeWishlist(w: WishlistItem): void {
    this.busy.set(true);
    this.api.removeWishlist(w.productId).subscribe({
      next: () => { this.ok('Removed from wishlist.'); this.wishlist.update((items) => items.filter((i) => i.productId !== w.productId)); },
      error: (err) => this.fail(err),
    });
  }
}
