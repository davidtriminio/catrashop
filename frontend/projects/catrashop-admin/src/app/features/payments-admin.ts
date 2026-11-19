import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { LocalDatePipe } from '@cs/util/local-date.pipe';
import { PaymentResponse } from '@cs/api/models';
import { PaymentsAdminApi } from '@cs/api/payments-admin.api';
import { StatusBadge } from '@cs/ui/status-badge';
import { MoneyPipe } from '@cs/util/money.pipe';

type Queue = 'review' | 'transfer' | 'collect' | 'done';

/**
 * Where money is checked and collected. A transfer arrives with a receipt that an operator verifies (approve → the
 * order is paid; reject → it goes back to the customer with the reason). Cash on delivery and pay-at-store orders are
 * already confirmed; their money is registered as collected once the order has been delivered.
 */
@Component({
  selector: 'app-payments-admin',
  imports: [RouterLink, LocalDatePipe, MoneyPipe, StatusBadge, TranslocoPipe],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ 'admin.nav.payments' | transloco }}</h1>
        <p class="muted count">{{ 'admin.payments.sub' | transloco }}</p>
      </div>
    </div>

    <div class="chips filters" role="group" [aria-label]="'admin.payments.queue' | transloco">
      @for (q of queues; track q.id) {
        <button class="chip" type="button" [class.active]="queue() === q.id" (click)="setQueue(q.id)">{{ q.label | transloco }}</button>
      }
    </div>

    @if (error()) { <div class="alert error">{{ error() }}</div> }
    @if (message()) { <div class="alert success">{{ message() }}</div> }

    <div class="card flush" [class.dim]="loading()">
      <table>
        <thead><tr><th>{{ 'admin.c.order' | transloco }}</th><th>{{ 'admin.c.date' | transloco }}</th><th>{{ 'admin.c.method' | transloco }}</th><th>{{ 'admin.orderDetail.transfer' | transloco }}</th><th>{{ 'admin.c.status' | transloco }}</th><th class="right">{{ 'admin.c.amount' | transloco }}</th><th></th></tr></thead>
        <tbody>
          @for (p of visible(); track p.id) {
            <tr>
              <td><a class="num" [routerLink]="['/orders', p.orderId]">{{ 'admin.payments.openOrder' | transloco }}</a></td>
              <td class="muted">{{ p.createdAtUtc | localDate: 'datetime' }}</td>
              <td>{{ methodLabel(p.method) }}</td>
              <td>
                @if (p.transferReference) {
                  <span>{{ p.transferBankAccountCode }} · <strong>{{ p.transferReference }}</strong></span>
                  <br /><span class="muted">{{ p.proofSubmittedAtUtc | localDate: 'datetime' }}</span>
                } @else { <span class="muted">–</span> }
              </td>
              <td><cs-status-badge [status]="p.status" /></td>
              <td class="right"><strong>{{ p.amount | money: p.currency }}</strong></td>
              <td class="right actions">
                @if (p.status === 'UnderReview') {
                  <button class="btn secondary sm" type="button" [disabled]="busy()" (click)="viewReceipt(p)">{{ 'admin.payments.receipt' | transloco }}</button>
                  <button class="btn sm" type="button" [disabled]="busy()" (click)="approve(p)">{{ 'admin.payments.approve' | transloco }}</button>
                  <button class="btn secondary sm danger-text" type="button" [disabled]="busy()" (click)="startReject(p)">{{ 'admin.payments.reject' | transloco }}</button>
                } @else if (p.status === 'Pending' && p.method !== 'TRANSFERENCIA') {
                  <button class="btn sm" type="button" [disabled]="busy()" (click)="collect(p)">{{ 'admin.payments.markCollected' | transloco }}</button>
                } @else if (p.hasReceipt) {
                  <button class="btn secondary sm" type="button" [disabled]="busy()" (click)="viewReceipt(p)">{{ 'admin.payments.receipt' | transloco }}</button>
                }
              </td>
            </tr>
            @if (rejecting() === p.id) {
              <tr class="reject-row">
                <td colspan="7">
                  <div class="reject">
                    <input class="input" [placeholder]="'admin.payments.rejectReason' | transloco" maxlength="500"
                           [value]="reason()" (input)="reason.set($any($event.target).value)" />
                    <button class="btn sm danger" type="button" [disabled]="busy() || !reason().trim()" (click)="confirmReject(p)">{{ 'admin.payments.sendBack' | transloco }}</button>
                    <button class="btn secondary sm" type="button" (click)="rejecting.set(null)">{{ 'admin.c.cancel' | transloco }}</button>
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
  private readonly i18n = inject(I18n);

  readonly queues: { id: Queue; label: string }[] = [
    { id: 'review', label: 'admin.payments.qReview' },
    { id: 'transfer', label: 'admin.payments.qTransfer' },
    { id: 'collect', label: 'admin.payments.qCollect' },
    { id: 'done', label: 'admin.payments.qDone' },
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

  methodLabel(method: string | null): string {
    this.i18n.lang(); // recompute when the language changes
    return method ? this.i18n.tOr(`admin.method.${method}`, method) : this.i18n.t('admin.method.gateway');
  }

  emptyText(): string {
    switch (this.queue()) {
      case 'review': return this.i18n.t('admin.payments.emptyReview');
      case 'transfer': return this.i18n.t('admin.payments.emptyTransfer');
      case 'collect': return this.i18n.t('admin.payments.emptyCollect');
      default: return this.i18n.t('admin.payments.emptyDone');
    }
  }

  setQueue(q: Queue): void { this.queue.set(q); this.rejecting.set(null); this.load(); }

  private load(): void {
    const status = { review: 'UnderReview', transfer: 'Pending', collect: 'Pending', done: 'Captured' }[this.queue()];
    this.loading.set(true);
    this.error.set(null);
    this.api.list(status).subscribe({
      next: (rows) => { this.all.set(rows); this.loading.set(false); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.loading.set(false); },
    });
  }

  viewReceipt(p: PaymentResponse): void {
    this.api.receipt(p.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener');
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (e) => this.error.set(this.i18n.problem(e, 'admin.payments.receiptError')),
    });
  }

  approve(p: PaymentResponse): void { this.run(this.api.approve(p.id), this.i18n.t('admin.payments.approved')); }
  collect(p: PaymentResponse): void { this.run(this.api.collect(p.id), this.i18n.t('admin.orderDetail.collectedDone')); }

  startReject(p: PaymentResponse): void { this.reason.set(''); this.rejecting.set(p.id); }
  confirmReject(p: PaymentResponse): void {
    this.run(this.api.reject(p.id, this.reason().trim()), this.i18n.t('admin.payments.rejected'));
    this.rejecting.set(null);
  }

  private run(op: ReturnType<PaymentsAdminApi['approve']>, done: string): void {
    this.busy.set(true); this.error.set(null); this.message.set(null);
    op.subscribe({
      next: () => { this.message.set(done); this.busy.set(false); this.load(); },
      error: (e) => { this.error.set(this.i18n.problem(e)); this.busy.set(false); },
    });
  }
}
