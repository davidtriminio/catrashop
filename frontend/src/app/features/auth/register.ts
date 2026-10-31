import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthStore } from '@cs/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { I18n } from '@cs/i18n/i18n';
import { PasswordInput } from '@cs/ui/password-input';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe, PasswordInput],
  template: `
    <div class="auth-wrap">
      <div class="card">
        <h1>{{ 'auth.register.title' | transloco }}</h1>
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="row">
            <div class="field" style="flex:1">
              <label for="firstName">{{ 'auth.firstName' | transloco }}</label>
              <input id="firstName" class="input" formControlName="firstName" autocomplete="given-name" />
            </div>
            <div class="field" style="flex:1">
              <label for="lastName">{{ 'auth.lastName' | transloco }}</label>
              <input id="lastName" class="input" formControlName="lastName" autocomplete="family-name" />
            </div>
          </div>
          <div class="field">
            <label for="email">{{ 'auth.email' | transloco }}</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">{{ 'auth.password' | transloco }}</label>
            <cs-password-input inputId="password" autocomplete="new-password" formControlName="password" />
            <small class="muted">{{ 'auth.register.passwordHint' | transloco }}</small>
          </div>
          <button class="btn block" type="submit" [disabled]="form.invalid || busy()">
            {{ (busy() ? 'auth.register.busy' : 'auth.register.submit') | transloco }}
          </button>
        </form>
        <p class="muted switch">{{ 'auth.register.haveAccount' | transloco }} <a routerLink="/login">{{ 'auth.login.title' | transloco }}</a></p>
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
  private readonly i18n = inject(I18n);
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
      error: (err) => { this.error.set(this.i18n.problem(err, 'auth.register.error')); this.busy.set(false); },
    });
  }
}
