import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthStore } from '@cs/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { I18n } from '@cs/i18n/i18n';
import { PasswordInput } from '@cs/ui/password-input';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe, PasswordInput],
  template: `
    <div class="auth-wrap">
      <div class="card">
        <h1>{{ 'auth.login.title' | transloco }}</h1>
        <p class="muted">{{ 'auth.login.welcome' | transloco }}</p>
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label for="email">{{ 'auth.email' | transloco }}</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">{{ 'auth.password' | transloco }}</label>
            <cs-password-input inputId="password" autocomplete="current-password" formControlName="password" />
          </div>
          <button class="btn block" type="submit" [disabled]="form.invalid || busy()">
            {{ (busy() ? 'auth.login.busy' : 'auth.login.submit') | transloco }}
          </button>
        </form>
        <p class="muted switch">{{ 'auth.login.noAccount' | transloco }} <a routerLink="/register">{{ 'auth.login.createOne' | transloco }}</a></p>
      </div>
    </div>
  `,
  styles: [`
    .auth-wrap { max-width: 400px; margin: 2rem auto; }
    .switch { margin-top: 1rem; text-align: center; }
  `],
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly i18n = inject(I18n);
  private readonly auth = inject(AuthStore);
  private readonly cart = inject(CartStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.busy.set(true);
    this.error.set(null);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.cart.refresh();
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/products';
        this.router.navigateByUrl(returnUrl);
      },
      error: (err) => { this.error.set(this.i18n.problem(err, 'auth.login.error')); this.busy.set(false); },
    });
  }
}
