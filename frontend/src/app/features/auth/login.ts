import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthStore } from '@ch/auth/auth.store';
import { CartStore } from '../../core/cart/cart.store';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrap">
      <div class="card">
        <h1>Sign in</h1>
        <p class="muted">Welcome back to CommerceHub.</p>
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label for="email">Email</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email" />
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input id="password" class="input" type="password" formControlName="password" autocomplete="current-password" />
          </div>
          <button class="btn block" type="submit" [disabled]="form.invalid || busy()">
            {{ busy() ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
        <p class="muted switch">No account? <a routerLink="/register">Create one</a></p>
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
      error: (err) => { this.error.set(problemMessage(err, 'Invalid email or password.')); this.busy.set(false); },
    });
  }
}
