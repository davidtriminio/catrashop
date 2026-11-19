import { Component, ElementRef, HostListener, computed, forwardRef, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Icon } from './icon';

export interface SelectOption { value: string | number; label: string; }

/**
 * Styled replacement for the native <select> (whose pop-up the browser draws and CSS cannot style).
 * Works with formControlName and ngModel; keyboard: arrows, Home/End, Enter/Space, Escape, type-to-jump.
 */
@Component({
  selector: 'cs-select',
  imports: [Icon],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Select), multi: true }],
  template: `
    <button type="button" class="trigger input" role="combobox" aria-haspopup="listbox"
            [attr.aria-expanded]="open()" [attr.aria-controls]="open() ? listId : null"
            [attr.aria-label]="ariaLabel() || null" [disabled]="disabled()"
            (click)="toggle()" (keydown)="onKey($event)">
      <span class="value" [class.placeholder]="!selected()">{{ selected()?.label ?? placeholder() }}</span>
      <cs-icon name="chevron" [size]="16" class="chev" />
    </button>
    @if (open()) {
      <ul class="menu" [class.up]="up()" role="listbox" [id]="listId" tabindex="-1">
        @for (o of options(); track o.value; let i = $index) {
          <li role="option" [attr.aria-selected]="isSelected(o)" [class.active]="i === active()" [class.on]="isSelected(o)"
              (mousedown)="$event.preventDefault()" (click)="choose(o)" (mouseenter)="active.set(i)">
            <span>{{ o.label }}</span>
            @if (isSelected(o)) { <cs-icon name="tick" [size]="14" /> }
          </li>
        }
      </ul>
    }
  `,
  styles: [`
    :host { position: relative; display: block; }
    .trigger { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); text-align: left; cursor: pointer; }
    .value { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .value.placeholder { color: var(--muted); }
    .chev { color: var(--muted); transition: transform 0.15s; }
    .trigger[aria-expanded='true'] .chev { transform: rotate(180deg); }
    .trigger[aria-expanded='true'] { border-color: var(--primary); box-shadow: var(--ring); background-color: var(--surface); }
    .menu {
      position: absolute; z-index: 40; left: 0; right: 0; top: calc(100% + 6px); margin: 0; padding: var(--space-1);
      list-style: none; max-height: 264px; overflow-y: auto; background: var(--surface);
      border: 1px solid var(--border-strong); border-radius: var(--radius-sm); box-shadow: var(--shadow-raised);
    }
    .menu.up { top: auto; bottom: calc(100% + 6px); }
    li { display: flex; align-items: center; justify-content: space-between; gap: var(--space-2); padding: 0.55rem 0.7rem; border-radius: 7px; cursor: pointer; font-size: 0.92rem; }
    li.active { background: var(--surface-2); }
    li.on { color: var(--primary); font-weight: 600; }
    :host-context(:root[data-theme='dark']) li.on { color: var(--accent-ink); }
  `],
})
export class Select implements ControlValueAccessor {
  private static nextId = 0;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly options = input.required<SelectOption[]>();
  readonly placeholder = input('');
  readonly ariaLabel = input('');

  readonly listId = `cs-select-${Select.nextId++}`;
  readonly open = signal(false);
  readonly up = signal(false);
  readonly active = signal(0);
  readonly disabled = signal(false);
  private readonly value = signal<string | number | null>(null);
  private typed = '';
  private typedAt = 0;

  readonly selected = computed(() => this.options().find((o) => String(o.value) === String(this.value())) ?? null);

  private onChange: (v: string | number) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(v: string | number | null): void { this.value.set(v); }
  registerOnChange(fn: (v: string | number) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(d: boolean): void { this.disabled.set(d); }

  isSelected(o: SelectOption): boolean { return String(o.value) === String(this.value()); }

  toggle(): void { this.open() ? this.close() : this.show(); }

  private show(): void {
    const rect = this.host.nativeElement.getBoundingClientRect();
    const below = window.innerHeight - rect.bottom;
    this.up.set(below < 280 && rect.top > below);
    const i = this.options().findIndex((o) => this.isSelected(o));
    this.active.set(i >= 0 ? i : 0);
    this.open.set(true);
  }

  private close(): void { this.open.set(false); this.onTouched(); }

  choose(o: SelectOption): void {
    this.value.set(o.value);
    this.onChange(o.value);
    this.close();
  }

  onKey(e: KeyboardEvent): void {
    const n = this.options().length;
    if (!n) return;
    switch (e.key) {
      case 'ArrowDown': case 'ArrowUp': {
        e.preventDefault();
        if (!this.open()) { this.show(); return; }
        this.active.set((this.active() + (e.key === 'ArrowDown' ? 1 : -1) + n) % n);
        break;
      }
      case 'Home': if (this.open()) { e.preventDefault(); this.active.set(0); } break;
      case 'End': if (this.open()) { e.preventDefault(); this.active.set(n - 1); } break;
      case 'Enter': case ' ':
        if (this.open()) { e.preventDefault(); this.choose(this.options()[this.active()]); }
        break;
      case 'Escape': if (this.open()) { e.preventDefault(); this.close(); } break;
      case 'Tab': if (this.open()) this.close(); break;
      default:
        if (e.key.length === 1 && !e.ctrlKey && !e.metaKey) {
          const now = Date.now();
          this.typed = now - this.typedAt > 700 ? e.key : this.typed + e.key;
          this.typedAt = now;
          const i = this.options().findIndex((o) => o.label.toLowerCase().startsWith(this.typed.toLowerCase()));
          if (i >= 0) { if (!this.open()) this.show(); this.active.set(i); }
        }
    }
  }

  @HostListener('document:mousedown', ['$event'])
  onOutside(e: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(e.target as Node)) this.close();
  }
}
