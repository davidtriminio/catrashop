import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShippingApi } from '@ch/api/shipping.api';
import { Tracking } from '@ch/api/models';
import { OrderStatusBadge } from '../orders/order-status-badge';
import { problemMessage } from '@ch/util/problem';

@Component({
  selector: 'app-tracking-page',
  imports: [FormsModule, DatePipe, OrderStatusBadge],
  template: `
    <h1>Track a shipment</h1>
    <div class="card">
      <div class="row">
        <input class="input" placeholder="Tracking number (e.g. CH…)" [(ngModel)]="trackingNumber" (keyup.enter)="track()" />
        <button class="btn" (click)="track()" [disabled]="!trackingNumber || busy()">Track</button>
      </div>
      @if (error()) { <div class="alert error">{{ error() }}</div> }
    </div>

    @if (result(); as t) {
      <div class="card">
        <p><strong>{{ t.trackingNumber }}</strong> · {{ t.carrier }} · <app-order-status-badge [status]="t.status" /></p>
        <p class="muted">Destination: {{ t.city }}, {{ t.country }}</p>
        <ul class="timeline">
          @for (e of t.events; track e.occurredAtUtc) {
            <li><strong>{{ e.status }}</strong> <span class="muted">{{ e.occurredAtUtc | date: 'short' }}</span>@if (e.note) { — {{ e.note }} }</li>
          }
        </ul>
      </div>
    }
  `,
  styles: [`
    .timeline { list-style: none; padding: 0; display: flex; flex-direction: column; gap: 0.4rem; }
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
