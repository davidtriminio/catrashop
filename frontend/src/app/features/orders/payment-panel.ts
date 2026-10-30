import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { BankAccount, OrderDetail, PaymentResponse } from '@cs/api/models';
import { PaymentsApi } from '@cs/api/payments.api';
import { I18n } from '@cs/i18n/i18n';
import { StatusBadge } from '@cs/ui/status-badge';

const MAX_RECEIPT_BYTES = 5 * 1024 * 1024;
const RECEIPT_TYPES = ['image/jpeg', 'image/png', 'application/pdf'];

/**
 * How the order is being paid. Cash on delivery and pay-at-store only inform (the order is already confirmed and the
 * money is collected later); a bank transfer shows the accounts and lets the customer attach the receipt, which an
 * operator verifies. An order from before payment methods existed shows nothing here (it keeps the "Pay now" button).
 */
@Component({
  selector: 'app-payment-panel',
  imports: [StatusBadge, TranslocoPipe],
  template: `
    @if (order().paymentMethod; as method) {
      <div class="card">
        <h2>{{ 'payment.panel.title' | transloco }}</h2>
        <p class="line"><span class="muted">{{ 'payment.panel.method' | transloco }}</span> <strong>{{ 'payment.method.' + method | transloco }}</strong></p>

        @if (!loaded()) {
          <p class="muted">{{ 'common.loading' | transloco }}</p>
        } @else if (!payment()) {
          <!-- The payment was not started (e.g. the connection dropped right after checkout). It is idempotent. -->
          @if (awaiting()) {
            @if (error()) { <div class="alert error">{{ error() }}</div> }
            <button class="btn" [disabled]="busy()" (click)="start()">{{ 'payment.panel.start' | transloco }}</button>
          }
        } @else if (payment(); as p) {
          <p class="line"><span class="muted">{{ 'payment.panel.status' | transloco }}</span> <cs-status-badge [status]="p.status" /></p>

          @if (method === 'TRANSFERENCIA') {
            @switch (p.status) {
              @case ('Captured') { <div class="alert success">{{ 'payment.panel.verified' | transloco }}</div> }
              @case ('UnderReview') { <div class="alert">{{ 'payment.panel.underReview' | transloco }}</div> }
              @default {
                @if (awaiting()) {
                  @if (p.reviewNote) { <div class="alert error">{{ 'payment.panel.rejected' | transloco: { reason: p.reviewNote } }}</div> }
                  <p class="muted">{{ 'payment.panel.transferIntro' | transloco }}</p>
                  @if (accounts().length === 0) {
                    <p class="muted">{{ 'payment.panel.noAccounts' | transloco }}</p>
                  } @else {
                    <ul class="accounts">
                      @for (a of accounts(); track a.code) {
                        <li>{{ 'payment.panel.account' | transloco: { bank: a.bank, type: a.accountType, number: a.accountNumber, holder: a.holder } }}</li>
                      }
                    </ul>
                    <form class="proof" (submit)="$event.preventDefault(); send(p)">
                      <div class="field">
                        <label for="bank">{{ 'payment.panel.bankAccount' | transloco }}</label>
                        <select id="bank" (change)="bank.set($any($event.target).value)">
                          @for (a of accounts(); track a.code) { <option [value]="a.code" [selected]="a.code === bank()">{{ a.bank }} · {{ a.accountNumber }}</option> }
                        </select>
                      </div>
                      <div class="field">
                        <label for="ref">{{ 'payment.panel.reference' | transloco }}</label>
                        <input id="ref" class="input" maxlength="100" [value]="reference()" (input)="reference.set($any($event.target).value)" />
                      </div>
                      <div class="field">
                        <label for="receipt">{{ 'payment.panel.receipt' | transloco }}</label>
                        <input id="receipt" class="input" type="file" accept="image/jpeg,image/png,application/pdf" (change)="pick($event)" />
                      </div>
                      @if (error()) { <div class="alert error">{{ error() }}</div> }
                      <button class="btn" type="submit" [disabled]="busy() || !reference().trim() || !file()">
                        {{ (busy() ? 'payment.panel.sending' : 'payment.panel.send') | transloco }}
                      </button>
                    </form>
                  }
                }
              }
            }
          } @else {
            @if (p.status === 'Captured') {
              <div class="alert success">{{ 'payment.panel.collected' | transloco }}</div>
            } @else if (awaiting() || order().status === 'Confirmed' || order().status === 'Processing' || order().status === 'Shipped') {
              <p class="muted">{{ (method === 'PAGO_EN_TIENDA' ? 'payment.panel.storePending' : 'payment.panel.cashPending') | transloco }}</p>
            }
          }
        }
      </div>
    }
  `,
  styles: [`
    .line { display: flex; align-items: center; flex-wrap: wrap; gap: var(--space-2); margin: 0 0 var(--space-3); }
    .accounts { margin: 0 0 var(--space-4); padding-left: 1.1rem; line-height: 1.6; }
    .proof .field { margin-bottom: var(--space-3); }
  `],
})
export class PaymentPanel {
  private readonly payments = inject(PaymentsApi);
  private readonly i18n = inject(I18n);

  readonly order = input.required<OrderDetail>();
  /** Emitted after the payment changed (started, receipt sent) so the page can reload the order. */
  readonly changed = output<void>();

  readonly payment = signal<PaymentResponse | null>(null);
  readonly accounts = signal<BankAccount[]>([]);
  readonly loaded = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly bank = signal('');
  readonly reference = signal('');
  readonly file = signal<File | null>(null);

  constructor() {
    effect(() => {
      const o = this.order();
      untracked(() => this.load(o));
    });
  }

  awaiting(): boolean {
    const s = this.order().status;
    return s === 'Pending' || s === 'PaymentPending' || s === 'PaymentFailed';
  }

  private load(o: OrderDetail): void {
    if (!o.paymentMethod) return;
    this.payments.forOrder(o.id).subscribe({
      next: (p) => { this.payment.set(p); this.loaded.set(true); },
      error: () => { this.payment.set(null); this.loaded.set(true); },
    });
    if (o.paymentMethod === 'TRANSFERENCIA') {
      this.payments.bankAccounts().subscribe({
        next: (a) => { this.accounts.set(a); if (!this.bank() && a.length) this.bank.set(a[0].code); },
        error: () => this.accounts.set([]),
      });
    }
  }

  start(): void {
    this.busy.set(true); this.error.set(null);
    this.payments.pay({ orderId: this.order().id }).subscribe({
      next: () => { this.busy.set(false); this.changed.emit(); },
      error: (err) => { this.error.set(this.i18n.problem(err, 'payment.panel.startFailed')); this.busy.set(false); },
    });
  }

  pick(event: Event): void {
    const picked = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.error.set(null);
    if (picked && picked.size > MAX_RECEIPT_BYTES) {
      this.file.set(null);
      this.error.set(this.i18n.t('payment.panel.tooLarge'));
      return;
    }
    if (picked && !RECEIPT_TYPES.includes(picked.type)) {
      this.file.set(null);
      this.error.set(this.i18n.t('payment.panel.wrongType'));
      return;
    }
    this.file.set(picked);
  }

  send(p: PaymentResponse): void {
    const receipt = this.file();
    if (!receipt) { this.error.set(this.i18n.t('payment.panel.needFile')); return; }
    this.busy.set(true); this.error.set(null);
    this.payments.submitTransferProof(p.id, this.bank(), this.reference().trim(), receipt).subscribe({
      next: (updated) => {
        this.payment.set(updated); this.file.set(null); this.reference.set(''); this.busy.set(false); this.changed.emit();
      },
      error: (err) => { this.error.set(this.i18n.problem(err, 'payment.panel.sendFailed')); this.busy.set(false); },
    });
  }
}
