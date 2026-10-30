import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NotificationsAdminApi } from '@ch/api/notifications-admin.api';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-notifications-admin',
  imports: [ReactiveFormsModule],
  template: `
    <h1>Send a notification</h1>
    <p class="muted">Operational/manual send. In-app is always delivered; email is sent when an address is given and the recipient allows it.</p>
    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <section class="card" style="max-width:640px">
      <form [formGroup]="form" (ngSubmit)="send()">
        <div class="field"><label>Recipient customer id (GUID)</label><input class="input" formControlName="recipientId" /></div>
        <div class="row">
          <div class="field" style="flex:1"><label>Type</label>
            <select formControlName="type">
              @for (t of types; track t.value) { <option [value]="t.value">{{ t.label }}</option> }
            </select>
          </div>
          <div class="field" style="flex:2"><label>Recipient email (optional)</label><input class="input" type="email" formControlName="recipientEmail" /></div>
        </div>
        <div class="field"><label>Message (for Promotion) / note</label><input class="input" formControlName="message" /></div>
        <button class="btn" [disabled]="form.invalid || busy()">Send</button>
      </form>
    </section>
  `,
})
export class NotificationsAdmin {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(NotificationsAdminApi);

  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  // NotificationType enum values (backend serialize as numbers).
  readonly types = [
    { label: 'Welcome', value: 0 },
    { label: 'Order placed', value: 10 },
    { label: 'Order paid', value: 11 },
    { label: 'Order shipped', value: 13 },
    { label: 'Order delivered', value: 14 },
    { label: 'Promotion', value: 50 },
    { label: 'Back in stock', value: 51 },
  ];

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
      data: v.message ? { message: v.message, title: 'A message from Catrashop' } : null,
    }).subscribe({
      next: () => { this.message.set('Notification dispatched.'); this.busy.set(false); this.form.reset({ type: 50 }); },
      error: (e) => { this.error.set(problemMessage(e)); this.busy.set(false); },
    });
  }
}
