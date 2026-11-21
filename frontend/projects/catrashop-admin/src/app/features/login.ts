import { Component, inject, signal } from '@angular/core';
import { BusyButton } from '../ui/busy-button';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthStore } from '@cs/auth/auth.store';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { PasswordInput } from '@cs/ui/password-input';
import { Icon } from '../ui/icon';

@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule, PasswordInput, Icon, TranslocoPipe, BusyButton],
  template: `
    <div class="split">
      <aside class="hero" aria-hidden="true">
        <div class="mark">
          <span class="logo"><cs-icon name="bag" [size]="20" /></span>
          <span class="nm">Catrashop <small>Admin</small></span>
        </div>
        <div class="pitch">
          <h2>{{ 'admin.login.pitch' | transloco }}</h2>
          <ul>
            <li><cs-icon name="bag" [size]="16" /> {{ 'admin.login.p1' | transloco }}</li>
            <li><cs-icon name="card" [size]="16" /> {{ 'admin.login.p2' | transloco }}</li>
            <li><cs-icon name="warehouse" [size]="16" /> {{ 'admin.login.p3' | transloco }}</li>
          </ul>
        </div>
        <small class="foot">{{ 'admin.login.staffOnly' | transloco }}</small>
      </aside>

      <main class="panel">
        <div class="box">
          <h1>{{ 'admin.login.title' | transloco }}</h1>
          <p class="muted">{{ 'admin.login.sub' | transloco }}</p>
          @if (error()) { <div class="alert error">{{ error() }}</div> }
          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="field"><label for="email">{{ 'admin.login.email' | transloco }}</label><input id="email" class="input" type="email" autocomplete="username" formControlName="email" /></div>
            <div class="field"><label for="password">{{ 'admin.login.password' | transloco }}</label><cs-password-input inputId="password" autocomplete="current-password" formControlName="password" /></div>
            <button class="btn block" type="submit" [csBusy]="busy()" [disabled]="form.invalid || busy()">{{ 'admin.login.title' | transloco }}</button>
          </form>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .split { display: grid; grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr); min-height: 100vh; }
    .hero {
      display: flex; flex-direction: column; justify-content: space-between; gap: var(--space-6); padding: var(--space-7);
      color: #dbe4ff;
      background:
        radial-gradient(900px 500px at 10% -10%, rgba(61, 107, 255, 0.45), transparent 60%),
        radial-gradient(700px 500px at 100% 110%, rgba(0, 51, 153, 0.7), transparent 60%),
        #0a1330;
    }
    .mark { display: flex; align-items: center; gap: var(--space-3); color: #fff; }
    .logo { display: grid; place-items: center; width: 40px; height: 40px; border-radius: 12px; background: linear-gradient(135deg, #3d6bff, #003399); box-shadow: 0 6px 18px rgba(0, 51, 153, 0.5); }
    .nm { font-weight: 800; font-size: 1.2rem; letter-spacing: -0.02em; display: flex; flex-direction: column; line-height: 1.1; }
    .nm small { font-size: 0.64rem; letter-spacing: 0.24em; text-transform: uppercase; color: #8fb0ff; margin-top: 2px; }
    .pitch h2 { color: #fff; font-size: 2.2rem; line-height: 1.12; letter-spacing: -0.03em; max-width: 14ch; margin-bottom: var(--space-5); }
    .pitch ul { list-style: none; margin: 0; padding: 0; display: grid; gap: var(--space-3); }
    .pitch li { display: flex; align-items: center; gap: var(--space-3); color: #b9c7ee; }
    .pitch li cs-icon { color: #8fb0ff; }
    .foot { color: rgba(185, 199, 238, 0.6); }
    .panel { display: grid; place-items: center; padding: var(--space-6) var(--space-4); }
    .box { width: 100%; max-width: 400px; }
    h1 { font-size: 1.8rem; margin-bottom: var(--space-1); }
    .muted { margin-bottom: var(--space-5); }
    .btn.block { padding: 0.8rem 1.1rem; margin-top: var(--space-2); }
    @media (max-width: 860px) {
      .split { grid-template-columns: 1fr; }
      .hero { padding: var(--space-5) var(--space-4); gap: var(--space-4); }
      .pitch, .foot { display: none; }
    }
  `],
})
export class AdminLogin {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly i18n = inject(I18n);

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
      error: (err) => { this.error.set(this.i18n.problem(err, 'admin.login.invalid')); this.busy.set(false); },
    });
  }
}
