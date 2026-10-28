import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { StatusBadge } from './status-badge';

export interface TimelineItem {
  status: string;
  occurredAtUtc: string;
  note?: string | null;
}

/** Vertical timeline, newest first, with the latest step emphasised. */
@Component({
  selector: 'ch-timeline',
  imports: [DatePipe, StatusBadge],
  template: `
    <ol class="timeline">
      @for (e of ordered(); track e.occurredAtUtc + e.status; let first = $first) {
        <li [class.current]="first">
          <span class="dot" aria-hidden="true"></span>
          <div class="body">
            <ch-status-badge [status]="e.status" />
            <time class="muted">{{ e.occurredAtUtc | date: 'MMM d, y, h:mm a' }}</time>
            @if (e.note) { <p class="note muted">{{ e.note }}</p> }
          </div>
        </li>
      } @empty { <li class="muted">Nothing recorded yet.</li> }
    </ol>
  `,
  styles: [`
    .timeline { list-style: none; margin: 0; padding: 0; }
    li { position: relative; display: flex; gap: var(--space-3); padding-bottom: var(--space-4); }
    li:last-child { padding-bottom: 0; }
    li:not(:last-child)::before { content: ''; position: absolute; left: 5px; top: 16px; bottom: 0; width: 2px; background: var(--border); }
    .dot { flex: none; width: 12px; height: 12px; margin-top: 5px; border-radius: 50%; background: var(--surface); border: 2px solid var(--border-strong); }
    li.current .dot { background: var(--accent); border-color: var(--accent-ink); }
    .body { display: flex; flex-wrap: wrap; align-items: center; gap: var(--space-1) var(--space-3); min-width: 0; }
    time { font-size: 0.82rem; }
    .note { flex-basis: 100%; margin: 0; font-size: 0.85rem; }
  `],
})
export class Timeline {
  readonly items = input.required<TimelineItem[]>();

  ordered(): TimelineItem[] {
    return [...this.items()].sort((a, b) => b.occurredAtUtc.localeCompare(a.occurredAtUtc));
  }
}
