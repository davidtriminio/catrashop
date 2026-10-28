import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ShippingApi } from '@ch/api/shipping.api';
import { Tracking } from '@ch/api/models';
import { StatusBadge } from '@ch/ui/status-badge';
import { Timeline } from '@ch/ui/timeline';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-tracking-page',
  imports: [FormsModule, StatusBadge, Timeline],
  template: `
    <div class="page-head"><h1>Track a shipment</h1></div>
    <div class="card">
      <div class="row finder">
        <input class="input" placeholder="Tracking number (e.g. CH…)" aria-label="Tracking number" [(ngModel)]="trackingNumber" (keyup.enter)="track()" />
        <button class="btn" (click)="track()" [disabled]="!trackingNumber || busy()">Track</button>
      </div>
      @if (error()) { <div class="alert error">{{ error() }}</div> }
    </div>

    @if (result(); as t) {
      <div class="card">
        <div class="head">
          <div>
            <strong class="num">{{ t.trackingNumber }}</strong>
            <p class="muted">{{ t.carrier }} · Destination: {{ t.city }}, {{ t.country }}</p>
          </div>
          <ch-status-badge [status]="t.status" />
        </div>
        <ch-timeline [items]="t.events" />
      </div>
    }
  `,
  styles: [`
    .finder .input { flex: 1; }
    .head { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--space-4); margin-bottom: var(--space-5); }
    .head p { margin: var(--space-1) 0 0; }
    .num { font-size: 1.1rem; }
    @media (max-width: 560px) { .finder { flex-direction: column; } }
  `],
})
export class TrackingPage {
  private readonly api = inject(ShippingApi);

  trackingNumber = '';
  readonly result = signal<Tracking | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  track(): void {
    if (!this.trackingNumber) return;
    this.busy.set(true);
    this.error.set(null);
    this.result.set(null);
    this.api.track(this.trackingNumber.trim()).subscribe({
      next: (t) => { this.result.set(t); this.busy.set(false); },
      error: (err) => { this.error.set(problemMessage(err, 'No shipment found for that tracking number.')); this.busy.set(false); },
    });
  }
}
