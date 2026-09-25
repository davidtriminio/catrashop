import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '@ch/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <div class="card">
        <h1>Create your account</h1>
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="row">
            <div class="field" style="flex:1">
              <label for="firstName">First name</label>
              <input id="firstName" class="input" formControlName="firstName" autocomplete="given-name" />
            </div>
            <div class="field" style="flex:1">
              <label for="lastName">Last name</label>
              <input id="lastName" class="input" formControlName="lastName" autocomplete="family-name" />
            </div>
          </div>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input id="password" class="input" type="password" formControlName="password" autocomplete="new-password" />
            <small class="muted">At least 8 characters.</small>
          </div>
          <button class="btn block" type="submit" [disabled]="form.invalid || busy()">
            {{ busy() ? 'Creating…' : 'Create account' }}
          </button>
        </form>
        <p class="muted switch">Already have an account? <a routerLink="/login">Sign in</a></p>
      </div>
    </div>
  `,
  styles: [`
    .auth-wrap { max-width: 440px; margin: 2rem auto; }
    .switch { margin-top: 1rem; text-align: center; }
  `],
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthStore);
  private readonly cart = inject(CartStore);
  private readonly router = inject(Router);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set(null);
    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => { this.cart.refresh(); this.router.navigateByUrl('/products'); },
      error: (err) => { this.error.set(problemMessage(err, 'Could not create the account.')); this.busy.set(false); },
    });
  }
}
