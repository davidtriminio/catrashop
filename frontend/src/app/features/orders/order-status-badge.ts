import { Component, computed, input } from '@angular/core';

/** Renders an order/shipment status as a coloured badge. */
@Component({
  selector: 'app-order-status-badge',
  template: `<span class="badge" [class.ok]="tone() === 'ok'" [class.warn]="tone() === 'warn'" [class.bad]="tone() === 'bad'">{{ status() }}</span>`,
})
export class OrderStatusBadge {
  readonly status = input.required<string>();

  readonly tone = computed<'ok' | 'warn' | 'bad' | 'default'>(() => {
    switch (this.status()) {
      case 'Paid': case 'Processing': case 'Shipped': case 'Delivered': return 'ok';
      case 'Pending': case 'PaymentPending': case 'RefundRequested': return 'warn';
      case 'PaymentFailed': case 'Cancelled': case 'Refunded': return 'bad';
      default: return 'default';
    }
  });
}
