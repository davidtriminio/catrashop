import { Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';

/** Password field with a show / hide toggle. Drop-in for `<input type="password" formControlName>` inside a reactive form. */
@Component({
  selector: 'cs-password-input',
  imports: [TranslocoPipe],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => PasswordInput), multi: true }],
  template: `
    <div class="pw">
      <input class="input" [id]="inputId()" [type]="visible() ? 'text' : 'password'" [value]="value()" [disabled]="disabled()"
             [attr.autocomplete]="autocomplete()" (input)="onInput($event)" (blur)="onTouched()" />
      <button class="toggle" type="button" (click)="visible.set(!visible())" [disabled]="disabled()"
              [attr.aria-label]="(visible() ? 'auth.hidePassword' : 'auth.showPassword') | transloco" [attr.aria-pressed]="visible()">
        @if (visible()) {
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M10.73 5.08A10.43 10.43 0 0 1 12 5c7 0 10 7 10 7a13.16 13.16 0 0 1-1.67 2.68"/>
            <path d="M6.61 6.61A13.53 13.53 0 0 0 2 12s3 7 10 7a9.74 9.74 0 0 0 5.39-1.61"/>
            <path d="M14.12 14.12a3 3 0 1 1-4.24-4.24"/><path d="m2 2 20 20"/>
          </svg>
        } @else {
          <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/>
          </svg>
        }
      </button>
    </div>
  `,
  styles: [`
    .pw { position: relative; }
    .pw .input { padding-right: 2.75rem; }
    .toggle {
      position: absolute; top: 50%; right: 0.4rem; transform: translateY(-50%);
      display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem;
      border: none; border-radius: 50%; background: none; color: var(--muted); cursor: pointer;
    }
    .toggle:hover { color: var(--primary); background: var(--tint-info-bg); }
  `],
})
export class PasswordInput implements ControlValueAccessor {
  readonly inputId = input('password');
  readonly autocomplete = input('current-password');

  readonly visible = signal(false);
  readonly value = signal('');
  readonly disabled = signal(false);

  private onChange: (v: string) => void = () => {};
  onTouched: () => void = () => {};

  onInput(e: Event): void {
    const v = (e.target as HTMLInputElement).value;
    this.value.set(v);
    this.onChange(v);
  }

  writeValue(v: string | null): void { this.value.set(v ?? ''); }
  registerOnChange(fn: (v: string) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(d: boolean): void { this.disabled.set(d); }
}
