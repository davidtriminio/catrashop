import { Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { I18n, LANGS, Lang } from '../i18n/i18n';

/** Small ES | EN toggle. The choice is saved, so the next visit opens in the same language. */
@Component({
  selector: 'ch-language-switcher',
  imports: [TranslocoPipe],
  template: `
    <div class="lang" role="group" [attr.aria-label]="'language.label' | transloco">
      @for (l of langs; track l) {
        <button type="button" [class.on]="i18n.lang() === l" [attr.aria-pressed]="i18n.lang() === l"
                [attr.lang]="l" [title]="'language.' + l | transloco" (click)="choose(l)">{{ l.toUpperCase() }}</button>
      }
    </div>
  `,
  styles: [`
    .lang { display: inline-flex; border: 1px solid var(--border-strong); border-radius: var(--radius-pill); overflow: hidden; }
    button { border: none; background: none; font: inherit; font-size: 0.75rem; font-weight: 700; letter-spacing: 0.04em; padding: 0.3rem 0.6rem; color: var(--muted); cursor: pointer; }
    button:hover { color: var(--text); }
    button.on { background: var(--primary); color: var(--primary-contrast); }
  `],
})
export class LanguageSwitcher {
  readonly i18n = inject(I18n);
  readonly langs = LANGS;

  choose(lang: Lang): void {
    void this.i18n.use(lang);
  }
}
