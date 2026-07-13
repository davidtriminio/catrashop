import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NotificationsApi } from '@ch/api/notifications.api';
import { Notification, NotificationPreferences } from '@ch/api/models';
import { problemMessage } from '@ch/util/problem';

interface Topic { label: string; value: number; name: string; }

@Component({
  selector: 'app-notifications-page',
  imports: [DatePipe],
  template: `
    <div class="head">
      <h1>Notifications</h1>
      <button class="btn secondary sm" (click)="markAll()" [disabled]="busy()">Mark all read</button>
    </div>
    @if (error()) { <div class="alert error">{{ error() }}</div> }

    <div class="card list">
      @for (n of items(); track n.id) {
        <div class="note" [class.unread]="!n.isRead">
          <div>
            <strong>{{ n.title }}</strong>
            <div class="muted">{{ n.body }}</div>
            <small class="muted">{{ n.createdAtUtc | date: 'medium' }}</small>
          </div>
          @if (!n.isRead) { <button class="btn secondary sm" (click)="markRead(n)">Mark read</button> }
        </div>
      } @empty { <p class="muted">No notifications yet.</p> }
    </div>

    <div class="card">
      <h2>Preferences</h2>
      <p class="muted">Transactional messages (orders, payments) are always sent. You can mute marketing messages per channel.</p>
      <table>
        <thead><tr><th>Topic</th>@for (c of channels; track c.value) { <th>{{ c.label }}</th> }</tr></thead>
        <tbody>
          @for (t of topics; track t.value) {
            <tr>
              <td>{{ t.label }}</td>
              @for (c of channels; track c.value) {
                <td>
                  <label class="mute">
                    <input type="checkbox" [checked]="isMuted(t, c)" (change)="toggle(t, c, $event)" /> Mute
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
    .head { display: flex; align-items: center; justify-content: space-between; }
    .list { padding: 0; }
    .note { display: flex; justify-content: space-between; align-items: center; gap: 1rem; padding: 0.8rem 1rem; border-bottom: 1px solid var(--border); }
    .note.unread { background: #f5f3ff; }
    .mute { font-size: 0.85rem; color: var(--muted); display: inline-flex; gap: 0.3rem; align-items: center; }
  `],
})
export class NotificationsPage implements OnInit {
  private readonly api = inject(NotificationsApi);

  readonly items = signal<Notification[]>([]);
  readonly preferences = signal<NotificationPreferences | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  // Backend enum values (NotificationType / NotificationChannel) sent as numbers on write.
  readonly topics: Topic[] = [
    { label: 'Promotions', value: 50, name: 'Promotion' },
    { label: 'Back in stock', value: 51, name: 'BackInStock' },
  ];
  readonly channels: Topic[] = [
    { label: 'In-app', value: 0, name: 'InApp' },
    { label: 'Email', value: 1, name: 'Email' },
  ];

  ngOnInit(): void {
    this.load();
    this.api.preferences().subscribe({ next: (p) => this.preferences.set(p), error: () => {} });
  }

  private load(): void {
    this.api.list(false, 1, 50).subscribe({
      next: (r) => this.items.set(r.items),
      error: (err) => this.error.set(problemMessage(err)),
    });
  }

  markRead(n: Notification): void {
    this.api.markRead(n.id).subscribe({
      next: () => this.items.update((list) => list.map((i) => (i.id === n.id ? { ...i, isRead: true } : i))),
      error: (err) => this.error.set(problemMessage(err)),
    });
  }

  markAll(): void {
    this.busy.set(true);
    this.api.markAllRead().subscribe({
      next: () => { this.items.update((list) => list.map((i) => ({ ...i, isRead: true }))); this.busy.set(false); },
      error: (err) => { this.error.set(problemMessage(err)); this.busy.set(false); },
    });
  }

  isMuted(topic: Topic, channel: Topic): boolean {
    return this.preferences()?.muted.some((m) => m.type === topic.name && m.channel === channel.name) ?? false;
  }

  toggle(topic: Topic, channel: Topic, event: Event): void {
    const muted = (event.target as HTMLInputElement).checked;
    this.api.updatePreference({ type: topic.value, channel: channel.value, muted }).subscribe({
      next: (p) => this.preferences.set(p),
      error: (err) => { this.error.set(problemMessage(err)); this.api.preferences().subscribe({ next: (p) => this.preferences.set(p) }); },
    });
  }
}
