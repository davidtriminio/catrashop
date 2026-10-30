import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthStore } from '@cs/auth/auth.store';
import { problemMessage } from '@cs/util/problem';

@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule],
  template: `
    <div class="wrap">
      <div class="brand">Catra<span>shop</span> <small>Admin</small></div>
      <div class="card">
        <h1>Sign in</h1>
        <p class="muted">Use a staff account to manage the store.</p>
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field"><label for="email">Email</label><input id="email" class="input" type="email" autocomplete="username" formControlName="email" /></div>
          <div class="field"><label for="password">Password</label><input id="password" class="input" type="password" autocomplete="current-password" formControlName="password" /></div>
          <button class="btn block" type="submit" [disabled]="form.invalid || busy()">{{ busy() ? 'Signing in…' : 'Sign in' }}</button>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .wrap { max-width: 420px; margin: var(--space-8) auto; padding: 0 var(--space-4); }
    .brand { text-align: center; font-weight: 800; font-size: 1.7rem; letter-spacing: -0.04em; margin-bottom: var(--space-5); }
    .brand span { background: var(--accent); color: var(--accent-ink); padding: 0 0.35rem; border-radius: 8px; margin-left: 1px; }
    .brand small { display: block; font-size: 0.7rem; font-weight: 650; letter-spacing: 0.25em; text-transform: uppercase; color: var(--muted); margin-top: var(--space-1); }
    h1 { margin-bottom: var(--space-1); }
    .muted { margin-bottom: var(--space-4); }
  `],
})
export class AdminLogin {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthStore);
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
      next: () => this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard'),
      error: (err) => { this.error.set(problemMessage(err, 'Invalid credentials.')); this.busy.set(false); },
    });
  }
}
