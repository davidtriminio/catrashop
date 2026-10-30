import { Component, computed, inject, input } from '@angular/core';
import { I18n } from '../i18n/i18n';

type Tone = 'accent' | 'warn' | 'info' | 'ok' | 'bad' | 'neutral';

/**
 * Order / payment / shipment status as a badge. One mapping for both apps so a state always looks the same:
 * lime = paid or confirmed and waiting for fulfilment, amber = being prepared or under review, indigo = on its way, green = done, red = failed.
 */
@Component({
  selector: 'cs-status-badge',
  template: `<span class="badge" [class]="tone() === 'neutral' ? '' : tone()">{{ label() }}</span>`,
})
export class StatusBadge {
  private readonly i18n = inject(I18n);

  readonly status = input.required<string>();

  /** Translated through `status.<camelCase>`; a status the server adds later still shows, in readable English. */
  readonly label = computed(() => {
    this.i18n.lang(); // recompute when the language changes
    const status = this.status();
    const spaced = status.replace(/([a-z])([A-Z])/g, '$1 $2');
    const readable = spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
    return this.i18n.tOr(`status.${status.charAt(0).toLowerCase()}${status.slice(1)}`, readable);
  });

  readonly tone = computed<Tone>(() => toneFor(this.status()));
}

export function toneFor(status: string): Tone {
  switch (status) {
    case 'Paid': case 'Confirmed': return 'accent';
    case 'Processing': case 'LabelCreated': case 'PickedUp': case 'UnderReview': return 'warn';
    case 'Shipped': case 'InTransit': case 'OutForDelivery': return 'info';
    case 'Delivered': case 'Captured': return 'ok';
    case 'PaymentFailed': case 'Cancelled': case 'Failed': case 'Returned': return 'bad';
    default: return 'neutral'; // Pending, PaymentPending, RefundRequested, Refunded, unknown
  }
}
