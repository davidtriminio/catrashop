import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NotificationsAdminApi } from '@cs/api/notifications-admin.api';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { Select } from '../ui/select';

@Component({
  selector: 'app-notifications-admin',
  imports: [ReactiveFormsModule, Select, TranslocoPipe],
  template: `
    <h1>{{ 'admin.notifications.title' | transloco }}</h1>
    <p class="muted">{{ 'admin.notifications.sub' | transloco }}</p>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card" style="max-width:640px">
      <form [formGroup]="form" (ngSubmit)="send()">
        <div class="field"><label>{{ 'admin.notifications.recipient' | transloco }}</label><input class="input" formControlName="recipientId" /></div>
        <div class="row">
          <div class="field" style="flex:1"><label>{{ 'admin.c.type' | transloco }}</label>
            <cs-select formControlName="type" [options]="types()" />
          </div>
          <div class="field" style="flex:2"><label>{{ 'admin.notifications.email' | transloco }}</label><input class="input" type="email" formControlName="recipientEmail" /></div>
        </div>
        <div class="field"><label>{{ 'admin.notifications.message' | transloco }}</label><input class="input" formControlName="message" /></div>
        <button class="btn" [disabled]="form.invalid || busy()">{{ 'admin.c.send' | transloco }}</button>
      </form>
    </section>
  `,
})
export class NotificationsAdmin {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(NotificationsAdminApi);
  private readonly i18n = inject(I18n);

  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  // NotificationType enum values (backend serialize as numbers).
  readonly types = computed(() => {
    this.i18n.lang(); // recompute when the language changes
    return [
      { value: 0, key: 'welcome' }, { value: 10, key: 'placed' }, { value: 11, key: 'paid' }, { value: 13, key: 'shipped' },
      { value: 14, key: 'delivered' }, { value: 50, key: 'promotion' }, { value: 51, key: 'backInStock' },
    ].map((t) => ({ value: t.value, label: this.i18n.t('admin.notifications.type.' + t.key) }));
  });

  readonly form = this.fb.nonNullable.group({
    recipientId: ['', Validators.required],
    type: [50, Validators.required],
    recipientEmail: [''],
    message: [''],
  });

  send(): void {
    if (this.form.invalid) return;
    this.busy.set(true); this.error.set(null); this.message.set(null);
    const v = this.form.getRawValue();
    this.api.send({
      recipientId: v.recipientId.trim(),
      type: Number(v.type),
      recipientEmail: v.recipientEmail || null,
      data: v.message ? { message: v.message, title: this.i18n.t('admin.notifications.defaultTitle') } : null,
    }).subscribe({
      next: () => { this.message.set(this.i18n.t('admin.notifications.sent')); this.busy.set(false); this.form.reset({ type: 50 }); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.busy.set(false); },
    });
  }
}
