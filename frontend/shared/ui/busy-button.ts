import { Directive, computed, effect, input, signal } from '@angular/core';

/**
 * Shows a small spinner on the button the person just pressed while its request runs, keeping the label's space so the
 * button never changes size. Pages keep one `busy` flag for the whole form; the spinner appears only on the button
 * that was actually pressed, not on its neighbours. Disabling the button is still the page's job (`[disabled]`).
 *
 *   <button class="btn" [csBusy]="busy()" (click)="save()">Save</button>
 */
@Directive({
  selector: 'button[csBusy]',
  host: {
    '[class.busy]': 'showing()',
    '[attr.aria-busy]': 'showing() || null',
    '(click)': 'pressed()',
  },
})
export class BusyButton {
  readonly csBusy = input(false);
  private readonly pending = signal(false);
  protected readonly showing = computed(() => this.csBusy() && this.pending());

  constructor() {
    // the request is over: forget the press
    effect(() => { if (!this.csBusy()) this.pending.set(false); });
  }

  protected pressed(): void {
    this.pending.set(true);
    // a press that did not start a request (invalid form, cancelled) must not light the spinner on a later one
    setTimeout(() => { if (!this.csBusy()) this.pending.set(false); }, 400);
  }
}
