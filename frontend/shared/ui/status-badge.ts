import { Component, computed, input } from '@angular/core';

type Tone = 'accent' | 'warn' | 'info' | 'ok' | 'bad' | 'neutral';

/**
 * Order / payment / shipment status as a badge. One mapping for both apps so a state always looks the same:
 * lime = paid and waiting for fulfilment, amber = being prepared, indigo = on its way, green = done, red = failed.
 */
@Component({
  selector: 'ch-status-badge',
  template: `<span class="badge" [class]="tone() === 'neutral' ? '' : tone()">{{ label() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<string>();

  readonly label = computed(() => {
    const spaced = this.status().replace(/([a-z])([A-Z])/g, '$1 $2');
    return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
  });

  readonly tone = computed<Tone>(() => toneFor(this.status()));
}

export function toneFor(status: string): Tone {
  switch (status) {
    case 'Paid': return 'accent';
    case 'Processing': case 'LabelCreated': case 'PickedUp': return 'warn';
    case 'Shipped': case 'InTransit': case 'OutForDelivery': return 'info';
    case 'Delivered': return 'ok';
    case 'PaymentFailed': case 'Cancelled': case 'Failed': case 'Returned': return 'bad';
    default: return 'neutral'; // Pending, PaymentPending, RefundRequested, Refunded, unknown
  }
}
