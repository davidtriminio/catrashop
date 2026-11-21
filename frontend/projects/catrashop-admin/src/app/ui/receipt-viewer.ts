import { Component, ElementRef, OnDestroy, OnInit, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { BusyButton } from './busy-button';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n } from '@cs/i18n/i18n';
import { PaymentResponse } from '@cs/api/models';
import { StatusBadge } from '@cs/ui/status-badge';
import { LocalDatePipe } from '@cs/util/local-date.pipe';
import { MoneyPipe } from '@cs/util/money.pipe';
import { Icon } from './icon';

const MIN = 0.5, MAX = 8, STEP = 1.2;

/**
 * Transfer receipt in a modal (no pop-up window): images get zoom (buttons, wheel, double click, keys) and drag-to-pan,
 * PDFs use the browser's own viewer. The payment's details sit beside it so the operator can verify, approve or send it
 * back without leaving the screen. The parent loads the file and owns the approve / reject calls.
 */
@Component({
  selector: 'app-receipt-viewer',
  imports: [Icon, TranslocoPipe, MoneyPipe, LocalDatePipe, StatusBadge, RouterLink, BusyButton],
  template: `
    <div class="overlay" (mousedown)="onBackdrop($event)">
      <div #dialog class="dialog" role="dialog" aria-modal="true" aria-labelledby="rv-title" tabindex="-1"
           (keydown)="onKey($event)">
        <header class="dialog-head">
          <h2 id="rv-title">{{ 'admin.receipt.title' | transloco }}</h2>
          <button type="button" class="icon-btn" [attr.aria-label]="'admin.receipt.close' | transloco" (click)="closed.emit()">
            <cs-icon name="close" />
          </button>
        </header>

        <div class="body">
          <div class="stage-wrap">
            @if (url(); as src) {
              @if (isPdf()) {
                <iframe class="pdf" [src]="safeUrl()" [title]="'admin.receipt.title' | transloco"></iframe>
              } @else {
                <div #stage class="stage" [class.dragging]="dragging()" (wheel)="onWheel($event)" (dblclick)="onDouble($event)"
                     (pointerdown)="down($event)" (pointermove)="move($event)" (pointerup)="up($event)" (pointercancel)="up($event)">
                  <img [src]="src" [alt]="'admin.receipt.title' | transloco" draggable="false"
                       [style.transform]="'translate(' + x() + 'px,' + y() + 'px) scale(' + scale() + ')'" />
                </div>
                <div class="tools" role="group" [attr.aria-label]="'admin.receipt.zoom' | transloco">
                  <button type="button" class="icon-btn" [attr.aria-label]="'admin.receipt.zoomOut' | transloco" (click)="zoom(1 / STEP)"><cs-icon name="zoomOut" /></button>
                  <span class="pct" aria-live="polite">{{ pct() }}%</span>
                  <button type="button" class="icon-btn" [attr.aria-label]="'admin.receipt.zoomIn' | transloco" (click)="zoom(STEP)"><cs-icon name="zoomIn" /></button>
                  <button type="button" class="icon-btn" [attr.aria-label]="'admin.receipt.fit' | transloco" (click)="reset()"><cs-icon name="fit" /></button>
                </div>
                <p class="hint">{{ 'admin.receipt.hint' | transloco }}</p>
              }
            } @else if (error()) {
              <p class="state alert error">{{ error() }}</p>
            } @else {
              <div class="load" role="status" aria-busy="true" [attr.aria-label]="'common.loading' | transloco"><span class="skeleton"></span></div>
            }
          </div>

          <aside class="info">
            <cs-status-badge [status]="payment().status" />
            <dl>
              <dt>{{ 'admin.c.amount' | transloco }}</dt><dd><strong>{{ payment().amount | money: payment().currency }}</strong></dd>
              <dt>{{ 'admin.c.method' | transloco }}</dt><dd>{{ methodLabel() }}</dd>
              @if (payment().transferReference) {
                <dt>{{ 'admin.receipt.reference' | transloco }}</dt><dd>{{ payment().transferReference }}</dd>
              }
              @if (payment().transferBankAccountCode) {
                <dt>{{ 'admin.receipt.account' | transloco }}</dt><dd>{{ payment().transferBankAccountCode }}</dd>
              }
              @if (payment().proofSubmittedAtUtc) {
                <dt>{{ 'admin.receipt.sent' | transloco }}</dt><dd>{{ payment().proofSubmittedAtUtc | localDate: 'datetime' }}</dd>
              }
            </dl>
            <a class="btn secondary sm" [routerLink]="['/orders', payment().orderId]" (click)="closed.emit()">{{ 'admin.payments.openOrder' | transloco }}</a>
            @if (url()) {
              <a class="btn secondary sm" [href]="url()" [attr.download]="'comprobante-' + payment().orderId.slice(0, 8)">
                <cs-icon name="download" [size]="14" /> {{ 'admin.receipt.download' | transloco }}
              </a>
            }

            @if (payment().status === 'UnderReview') {
              <div class="decide">
                @if (rejecting()) {
                  <textarea class="input" rows="3" maxlength="500" [value]="reason()" (input)="reason.set($any($event.target).value)"
                            [placeholder]="'admin.payments.rejectReason' | transloco"></textarea>
                  <button type="button" class="btn danger" [csBusy]="busy()" [disabled]="busy() || !reason().trim()" (click)="rejected.emit(reason().trim())">{{ 'admin.payments.sendBack' | transloco }}</button>
                  <button type="button" class="btn secondary" (click)="rejecting.set(false)">{{ 'admin.c.cancel' | transloco }}</button>
                } @else {
                  <button type="button" class="btn" [csBusy]="busy()" [disabled]="busy()" (click)="approved.emit()">{{ 'admin.payments.approve' | transloco }}</button>
                  <button type="button" class="btn secondary danger-text" [csBusy]="busy()" [disabled]="busy()" (click)="rejecting.set(true)">{{ 'admin.payments.reject' | transloco }}</button>
                }
              </div>
            }
          </aside>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .body { display: grid; grid-template-columns: minmax(0, 1fr) 260px; min-height: 0; flex: 1; }
    .stage-wrap { position: relative; display: flex; min-height: 0; background: var(--surface-2); }
    .stage { flex: 1; display: grid; place-items: center; overflow: hidden; cursor: grab; touch-action: none; user-select: none; }
    .stage.dragging { cursor: grabbing; }
    .stage img { max-width: 100%; max-height: 100%; object-fit: contain; transform-origin: center; will-change: transform; pointer-events: none; }
    .pdf { flex: 1; width: 100%; border: 0; background: #fff; }
    .state { margin: auto; padding: var(--space-4); }
    .load { flex: 1; display: grid; place-items: center; }
    .load .skeleton { width: min(60%, 320px); height: 72%; border-radius: var(--radius-sm); }
    .tools { position: absolute; left: 50%; bottom: var(--space-5); translate: -50% 0; display: flex; align-items: center; gap: var(--space-1); padding: var(--space-1); border-radius: var(--radius-sm); background: var(--surface); border: 1px solid var(--border); box-shadow: var(--shadow-raised); }
    .pct { min-width: 3.2rem; text-align: center; font-size: 0.8rem; font-weight: 650; }
    .hint { position: absolute; top: var(--space-3); left: 0; right: 0; margin: 0; text-align: center; font-size: 0.75rem; color: var(--muted); pointer-events: none; }
    .info { display: flex; flex-direction: column; align-items: flex-start; gap: var(--space-3); padding: var(--space-4); border-left: 1px solid var(--border); overflow-y: auto; }
    dl { display: grid; gap: 2px; margin: 0; width: 100%; }
    dt { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--muted); margin-top: var(--space-2); }
    dd { margin: 0; overflow-wrap: anywhere; }
    .decide { display: flex; flex-direction: column; gap: var(--space-2); width: 100%; margin-top: auto; padding-top: var(--space-3); }
    .danger-text { color: var(--danger); }
    @media (max-width: 800px) {
      .body { grid-template-columns: 1fr; grid-template-rows: minmax(260px, 1fr) auto; overflow-y: auto; }
      .info { border-left: 0; border-top: 1px solid var(--border); }
    }
  `],
})
export class ReceiptViewer implements OnInit, OnDestroy {
  private readonly i18n = inject(I18n);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');
  private readonly stage = viewChild<ElementRef<HTMLElement>>('stage');
  private opener: Element | null = null;

  readonly payment = input.required<PaymentResponse>();
  /** The receipt file, once the parent has fetched it. */
  readonly receipt = input<Blob | null>(null);
  readonly error = input<string | null>(null);
  readonly busy = input(false);
  readonly closed = output<void>();
  readonly approved = output<void>();
  readonly rejected = output<string>();

  readonly STEP = STEP;
  readonly url = signal<string | null>(null);
  readonly isPdf = computed(() => this.receipt()?.type === 'application/pdf');
  readonly safeUrl = computed(() => this.sanitizer.bypassSecurityTrustResourceUrl(this.url() ?? '')); // our own blob: URL
  readonly scale = signal(1);
  readonly x = signal(0);
  readonly y = signal(0);
  readonly dragging = signal(false);
  readonly pct = computed(() => Math.round(this.scale() * 100));
  readonly rejecting = signal(false);
  readonly reason = signal('');
  private last: { x: number; y: number } | null = null;

  constructor() {
    effect((onCleanup) => {
      const blob = this.receipt();
      if (!blob) { this.url.set(null); return; }
      const u = URL.createObjectURL(blob);
      this.url.set(u);
      this.reset();
      onCleanup(() => URL.revokeObjectURL(u));
    });
  }

  methodLabel(): string {
    this.i18n.lang(); // recompute when the language changes
    const m = this.payment().method;
    return m ? this.i18n.tOr(`admin.method.${m}`, m) : this.i18n.t('admin.method.gateway');
  }

  ngOnInit(): void {
    this.opener = document.activeElement;
    document.body.style.overflow = 'hidden';
    queueMicrotask(() => this.dialog().nativeElement.focus());
  }

  ngOnDestroy(): void {
    document.body.style.overflow = '';
    (this.opener as HTMLElement | null)?.focus?.();
  }

  zoom(factor: number, at?: { x: number; y: number }): void {
    const s = this.scale();
    const next = Math.min(MAX, Math.max(MIN, s * factor));
    if (next === s) return;
    const k = next / s;
    // keep the point under the cursor (relative to the stage centre) where it is
    const p = at ?? { x: 0, y: 0 };
    this.x.set(p.x - (p.x - this.x()) * k);
    this.y.set(p.y - (p.y - this.y()) * k);
    this.scale.set(next);
    if (next <= 1) { this.x.set(0); this.y.set(0); }
  }

  reset(): void { this.scale.set(1); this.x.set(0); this.y.set(0); }

  private centred(e: MouseEvent): { x: number; y: number } {
    const r = this.stage()!.nativeElement.getBoundingClientRect();
    return { x: e.clientX - (r.left + r.width / 2), y: e.clientY - (r.top + r.height / 2) };
  }

  onWheel(e: WheelEvent): void {
    e.preventDefault();
    this.zoom(e.deltaY < 0 ? STEP : 1 / STEP, this.centred(e));
  }

  onDouble(e: MouseEvent): void {
    if (this.scale() > 1) this.reset(); else this.zoom(2.5, this.centred(e));
  }

  down(e: PointerEvent): void {
    this.dragging.set(true);
    this.last = { x: e.clientX, y: e.clientY };
    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
  }

  move(e: PointerEvent): void {
    if (!this.dragging() || !this.last) return;
    this.x.set(this.x() + e.clientX - this.last.x);
    this.y.set(this.y() + e.clientY - this.last.y);
    this.last = { x: e.clientX, y: e.clientY };
  }

  up(e: PointerEvent): void {
    this.dragging.set(false);
    this.last = null;
    (e.currentTarget as HTMLElement).releasePointerCapture?.(e.pointerId);
  }

  onBackdrop(e: MouseEvent): void {
    if (e.target === e.currentTarget) this.closed.emit();
  }

  onKey(e: KeyboardEvent): void {
    if (e.key === 'Escape') { e.stopPropagation(); this.closed.emit(); return; }
    if (e.key === 'Tab') this.trap(e);
    const t = e.target as HTMLElement;
    if (t.tagName === 'TEXTAREA' || t.tagName === 'INPUT') return;
    if (e.key === '+' || e.key === '=') this.zoom(STEP);
    else if (e.key === '-') this.zoom(1 / STEP);
    else if (e.key === '0') this.reset();
  }

  /** Keep Tab inside the dialog. */
  private trap(e: KeyboardEvent): void {
    const nodes = Array.from(this.dialog().nativeElement.querySelectorAll<HTMLElement>('button:not([disabled]), a[href], textarea, iframe'));
    if (!nodes.length) return;
    const first = nodes[0], last = nodes[nodes.length - 1];
    const active = document.activeElement;
    if (e.shiftKey && (active === first || active === this.dialog().nativeElement)) { e.preventDefault(); last.focus(); }
    else if (!e.shiftKey && active === last) { e.preventDefault(); first.focus(); }
  }
}
