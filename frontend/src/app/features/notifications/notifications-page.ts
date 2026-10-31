import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { NotificationsApi } from '@cs/api/notifications.api';
import { Notification, NotificationPreferences } from '@cs/api/models';
import { I18n } from '@cs/i18n/i18n';
import { LocalDatePipe } from '@cs/util/local-date.pipe';

interface Topic { labelKey: string; value: number; name: string; }

@Component({
  selector: 'app-notifications-page',
  imports: [LocalDatePipe, TranslocoPipe],
  template: `
    <div class="page-head">
      <h1>{{ 'notifications.title' | transloco }}</h1>
      <button class="btn secondary sm" (click)="markAll()" [disabled]="busy()">{{ 'notifications.markAll' | transloco }}</button>
    </div>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    <div class="card flush">
      @for (n of items(); track n.id) {
        <div class="list-row" [class.unread]="!n.isRead">
          <div class="note-text">
            <strong>{{ n.title }}</strong>
            <div class="muted">{{ n.body }}</div>
            <small class="muted">{{ n.createdAtUtc | localDate: 'datetime' }}</small>
          </div>
          @if (!n.isRead) { <button class="btn secondary sm" (click)="markRead(n)">{{ 'notifications.markRead' | transloco }}</button> }
        </div>
      } @empty { <p class="empty">{{ 'notifications.empty' | transloco }}</p> }
    </div>

    <div class="card">
      <h2>{{ 'notifications.preferences' | transloco }}</h2>
      <p class="muted">{{ 'notifications.preferencesNote' | transloco }}</p>
      <table>
        <thead><tr><th>{{ 'notifications.topic' | transloco }}</th>@for (c of channels; track c.value) { <th>{{ c.labelKey | transloco }}</th> }</tr></thead>
        <tbody>
          @for (t of topics; track t.value) {
            <tr>
              <td>{{ t.labelKey | transloco }}</td>
              @for (c of channels; track c.value) {
                <td>
                  <label class="switch" [class.off]="isMuted(t, c)">
                    <input type="checkbox" role="switch" [checked]="!isMuted(t, c)" (change)="toggle(t, c, $event)"
                           [attr.aria-label]="(c.labelKey | transloco) + ': ' + (t.labelKey | transloco)" />
                    <span class="track" aria-hidden="true"></span>
                    <span class="state">{{ (isMuted(t, c) ? 'notifications.muted' : 'notifications.active') | transloco }}</span>
                  </label>
                </td>
              }
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .note-text { display: flex; flex-direction: column; gap: var(--space-1); min-width: 0; }
    .note-text small { margin-top: var(--space-1); }
    .switch { display: inline-flex; align-items: center; gap: var(--space-3); cursor: pointer; font-size: 0.85rem; font-weight: 600; color: var(--primary); }
    .switch.off { color: var(--muted); }
    .switch input { position: absolute; opacity: 0; width: 1px; height: 1px; }
    .track { position: relative; width: 40px; height: 22px; border-radius: var(--radius-pill); background: var(--primary); transition: background 0.15s; flex-shrink: 0; }
    .track::after { content: ''; position: absolute; top: 3px; left: 21px; width: 16px; height: 16px; border-radius: 50%; background: #fff; transition: left 0.15s; }
    .switch.off .track { background: var(--border-strong); }
    .switch.off .track::after { left: 3px; }
    .switch input:focus-visible + .track { outline: 2px solid var(--primary); outline-offset: 2px; }
  `],
})
export class NotificationsPage implements OnInit {
  private readonly api = inject(NotificationsApi);
  private readonly i18n = inject(I18n);

  readonly items = signal<Notification[]>([]);
  readonly preferences = signal<NotificationPreferences | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  // Backend enum values (NotificationType / NotificationChannel) sent as numbers on write.
  readonly topics: Topic[] = [
    { labelKey: 'notifications.topics.promotion', value: 50, name: 'Promotion' },
    { labelKey: 'notifications.topics.backInStock', value: 51, name: 'BackInStock' },
  ];
  readonly channels: Topic[] = [
    { labelKey: 'notifications.channels.inApp', value: 0, name: 'InApp' },
    { labelKey: 'notifications.channels.email', value: 1, name: 'Email' },
  ];

  ngOnInit(): void {
    this.load();
    this.api.preferences().subscribe({ next: (p) => this.preferences.set(p), error: () => {} });
  }

  private load(): void {
    this.api.list(false, 1, 50).subscribe({
      next: (r) => this.items.set(r.items),
      error: (err) => this.error.set(this.i18n.problem(err)),
    });
  }

  markRead(n: Notification): void {
    this.api.markRead(n.id).subscribe({
      next: () => this.items.update((list) => list.map((i) => (i.id === n.id ? { ...i, isRead: true } : i))),
      error: (err) => this.error.set(this.i18n.problem(err)),
    });
  }

  markAll(): void {
    this.busy.set(true);
    this.api.markAllRead().subscribe({
      next: () => { this.items.update((list) => list.map((i) => ({ ...i, isRead: true }))); this.busy.set(false); },
      error: (err) => { this.error.set(this.i18n.problem(err)); this.busy.set(false); },
    });
  }

  isMuted(topic: Topic, channel: Topic): boolean {
    return this.preferences()?.muted.some((m) => m.type === topic.name && m.channel === channel.name) ?? false;
  }

  toggle(topic: Topic, channel: Topic, event: Event): void {
    const muted = !(event.target as HTMLInputElement).checked;
    this.api.updatePreference({ type: topic.value, channel: channel.value, muted }).subscribe({
      next: (p) => this.preferences.set(p),
      error: (err) => { this.error.set(this.i18n.problem(err)); this.api.preferences().subscribe({ next: (p) => this.preferences.set(p) }); },
    });
  }
}
