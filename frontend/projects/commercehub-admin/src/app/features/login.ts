import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthStore } from '@ch/auth/auth.store';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule],
  template: `
    <div class="wrap">
      <div class="card">
        <h1>Admin console</h1>
        <p class="muted">Sign in with a staff account.</p>
        @if (error()) { <div class="alert error">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field"><label for="email">Email</label><input id="email" class="input" type="email" formControlName="email" /></div>
          <div class="field"><label for="password">Password</label><input id="password" class="input" type="password" formControlName="password" /></div>
          <button class="btn block" type="submit" [disabled]="form.invalid || busy()">{{ busy() ? 'Signing in…' : 'Sign in' }}</button>
        </form>
      </div>
    </div>
  `,
  styles: [`.wrap { max-width: 400px; margin: 4rem auto; } h1 { margin-bottom: 0.25rem; }`],
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
