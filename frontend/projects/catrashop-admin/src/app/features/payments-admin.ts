import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PaymentResponse } from '@cs/api/models';
import { PaymentsAdminApi } from '@cs/api/payments-admin.api';
import { StatusBadge } from '@cs/ui/status-badge';
import { MoneyPipe } from '@cs/util/money.pipe';
import { problemMessage } from '@cs/util/problem';

type Queue = 'review' | 'transfer' | 'collect' | 'done';

const METHOD_LABEL: Record<string, string> = {
  CONTRA_ENTREGA: 'Cash on delivery',
  TRANSFERENCIA: 'Bank transfer',
  PAGO_EN_TIENDA: 'Pay at the store',
};

/**
 * Where money is checked and collected. A transfer arrives with a receipt that an operator verifies (approve → the
 * order is paid; reject → it goes back to the customer with the reason). Cash on delivery and pay-at-store orders are
 * already confirmed; their money is registered as collected once the order has been delivered.
 */
@Component({
  selector: 'app-payments-admin',
  imports: [RouterLink, DatePipe, MoneyPipe, StatusBadge],
  template: `
    <div class="page-head">
      <div>
        <h1>Payments</h1>
        <p class="muted count">Verify transfers and register the money collected on delivery or in the store.</p>
      </div>
    </div>

    <div class="chips filters" role="group" aria-label="Queue">
      @for (q of queues; track q.id) {
        <button class="chip" type="button" [class.active]="queue() === q.id" (click)="setQueue(q.id)">{{ q.label }}</button>
      }
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <div class="card flush" [class.dim]="loading()">
      <table>
        <thead><tr><th>Order</th><th>Date</th><th>Method</th><th>Transfer</th><th>Status</th><th class="right">Amount</th><th></th></tr></thead>
        <tbody>
          @for (p of visible(); track p.id) {
            <tr>
              <td><a class="num" [routerLink]="['/orders', p.orderId]">Open order</a></td>
              <td class="muted">{{ p.createdAtUtc | date: 'MMM d, y, h:mm a' }}</td>
              <td>{{ methodLabel(p.method) }}</td>
              <td>
                @if (p.transferReference) {
                  <span>{{ p.transferBankAccountCode }} · <strong>{{ p.transferReference }}</strong></span>
                  <br /><span class="muted">{{ p.proofSubmittedAtUtc | date: 'MMM d, h:mm a' }}</span>
                } @else { <span class="muted">–</span> }
              </td>
              <td><cs-status-badge [status]="p.status" /></td>
              <td class="right"><strong>{{ p.amount | money: p.currency }}</strong></td>
              <td class="right actions">
                @if (p.status === 'UnderReview') {
                  <button class="btn secondary sm" type="button" [disabled]="busy()" (click)="viewReceipt(p)">Receipt</button>
                  <button class="btn sm" type="button" [disabled]="busy()" (click)="approve(p)">Approve</button>
                  <button class="btn secondary sm danger-text" type="button" [disabled]="busy()" (click)="startReject(p)">Reject</button>
                } @else if (p.status === 'Pending' && p.method !== 'TRANSFERENCIA') {
                  <button class="btn sm" type="button" [disabled]="busy()" (click)="collect(p)">Mark collected</button>
                } @else if (p.hasReceipt) {
                  <button class="btn secondary sm" type="button" [disabled]="busy()" (click)="viewReceipt(p)">Receipt</button>
                }
              </td>
            </tr>
            @if (rejecting() === p.id) {
              <tr class="reject-row">
                <td colspan="7">
                  <div class="reject">
                    <input class="input" placeholder="Why can't it be verified? The customer will see this." maxlength="500"
                           [value]="reason()" (input)="reason.set($any($event.target).value)" />
                    <button class="btn sm danger" type="button" [disabled]="busy() || !reason().trim()" (click)="confirmReject(p)">Send back</button>
                    <button class="btn secondary sm" type="button" (click)="rejecting.set(null)">Cancel</button>
                  </div>
                </td>
              </tr>
            }
          } @empty {
            <tr><td colspan="7" class="empty">{{ emptyText() }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    .count { margin: var(--space-1) 0 0; }
    .filters { margin-bottom: var(--space-5); }
    .dim { opacity: 0.6; }
    .num { font-weight: 650; white-space: nowrap; }
    td { white-space: nowrap; }
    .actions { display: flex; gap: var(--space-2); justify-content: flex-end; }
    .danger-text { color: var(--danger); }
    .reject { display: flex; gap: var(--space-2); align-items: center; }
    .reject .input { flex: 1; min-width: 0; }
    .card.flush { overflow-x: auto; }
  `],
})
export class PaymentsAdmin implements OnInit {
  private readonly api = inject(PaymentsAdminApi);

  readonly queues: { id: Queue; label: string }[] = [
    { id: 'review', label: 'Receipts to verify' },
    { id: 'transfer', label: 'Awaiting transfer' },
    { id: 'collect', label: 'To collect' },
    { id: 'done', label: 'Collected / verified' },
  ];

  readonly queue = signal<Queue>('review');
  private readonly all = signal<PaymentResponse[]>([]);
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly rejecting = signal<string | null>(null);
  readonly reason = signal('');

  readonly visible = computed(() => {
    const rows = this.all();
    switch (this.queue()) {
      case 'transfer': return rows.filter((p) => p.method === 'TRANSFERENCIA');
      case 'collect': return rows.filter((p) => p.method !== 'TRANSFERENCIA');
      default: return rows;
    }
  });

  ngOnInit(): void { this.load(); }

  methodLabel(method: string | null): string { return method ? (METHOD_LABEL[method] ?? method) : 'Gateway'; }

  emptyText(): string {
    switch (this.queue()) {
      case 'review': return 'No receipts are waiting for verification.';
      case 'transfer': return 'No customer is waiting to send a transfer receipt.';
      case 'collect': return 'Nothing to collect.';
      default: return 'Nothing here yet.';
    }
  }

  setQueue(q: Queue): void { this.queue.set(q); this.rejecting.set(null); this.load(); }

  private load(): void {
    const status = { review: 'UnderReview', transfer: 'Pending', collect: 'Pending', done: 'Captured' }[this.queue()];
    this.loading.set(true);
    this.error.set(null);
    this.api.list(status).subscribe({
      next: (rows) => { this.all.set(rows); this.loading.set(false); },
      error: (e) => { this.error.set(problemMessage(e)); this.loading.set(false); },
    });
  }

  viewReceipt(p: PaymentResponse): void {
    this.api.receipt(p.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (e) => this.error.set(problemMessage(e, 'The receipt could not be opened.')),
    });
  }

  approve(p: PaymentResponse): void { this.run(this.api.approve(p.id), 'Transfer verified; the order is now paid.'); }
  collect(p: PaymentResponse): void { this.run(this.api.collect(p.id), 'Payment registered as collected.'); }

  startReject(p: PaymentResponse): void { this.reason.set(''); this.rejecting.set(p.id); }
  confirmReject(p: PaymentResponse): void {
    this.run(this.api.reject(p.id, this.reason().trim()), 'Receipt sent back to the customer.');
    this.rejecting.set(null);
  }

  private run(op: ReturnType<PaymentsAdminApi['approve']>, done: string): void {
    this.busy.set(true); this.error.set(null); this.message.set(null);
    op.subscribe({
      next: () => { this.message.set(done); this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(problemMessage(e)); this.busy.set(false); },
    });
  }
}
