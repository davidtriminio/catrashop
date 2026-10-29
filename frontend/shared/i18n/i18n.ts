import { registerLocaleData } from '@angular/common';
import localeEsHn from '@angular/common/locales/es-HN';
import {
  EnvironmentProviders,
  Injectable,
  LOCALE_ID,
  inject,
  isDevMode,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoLoader, TranslocoService, provideTransloco } from '@jsverse/transloco';
import { firstValueFrom, from } from 'rxjs';
import { problemMessage } from '../util/problem';

/** Languages the apps can show. Spanish is the main one; English is the fallback. Add a code here and a JSON file to add more. */
export const LANGS = ['es', 'en'] as const;
export type Lang = (typeof LANGS)[number];

export const DEFAULT_LANG: Lang = 'es';
export const FALLBACK_LANG: Lang = 'en';

/** Locale used to format dates for each language. */
export const LOCALES: Readonly<Record<Lang, string>> = { es: 'es-HN', en: 'en-US' };

const STORAGE_KEY = 'ch_lang';

const translations: Record<Lang, () => Promise<{ default: Record<string, unknown> }>> = {
  es: () => import('./es.json'),
  en: () => import('./en.json'),
};

/** Translation files are bundled with the app (no extra request, no flash of untranslated keys). */
class BundledLoader implements TranslocoLoader {
  getTranslation(lang: string) {
    return from(translations[lang as Lang]().then((m) => m.default));
  }
}

export function isLang(value: unknown): value is Lang {
  return typeof value === 'string' && (LANGS as readonly string[]).includes(value);
}

function readStoredLang(): Lang | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    return isLang(stored) ? stored : null;
  } catch {
    return null;
  }
}

/** Active language, translations for code, and the persisted choice. Templates use the `transloco` pipe instead. */
@Injectable({ providedIn: 'root' })
export class I18n {
  private readonly transloco = inject(TranslocoService);

  readonly lang = toSignal(this.transloco.langChanges$, { initialValue: this.transloco.getActiveLang() });

  /** Loads and activates the stored language (or the app default). Runs before the first render. */
  async init(defaultLang: Lang): Promise<void> {
    await this.apply(readStoredLang() ?? defaultLang);
  }

  /** Switches language at runtime and remembers it for the next visit. */
  async use(lang: Lang): Promise<void> {
    await this.apply(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      /* private mode: the choice just lasts for this visit */
    }
  }

  t(key: string, params?: Record<string, unknown>): string {
    return this.transloco.translate(key, params);
  }

  /** Translates `key`, or returns `fallback` when no translation exists (e.g. a status the server added later). */
  tOr(key: string, fallback: string): string {
    const text = this.transloco.translate(key);
    return text === key ? fallback : text;
  }

  /** Message for an API error: the server's own text when it sent one, otherwise the translated `fallbackKey`. */
  problem(error: unknown, fallbackKey = 'errors.generic'): string {
    return problemMessage(error, this.t(fallbackKey), this.t('errors.offline'));
  }

  private async apply(lang: Lang): Promise<void> {
    await firstValueFrom(this.transloco.load(lang));
    this.transloco.setActiveLang(lang);
    document.documentElement.lang = lang;
  }
}

/**
 * Registers Transloco with the bundled es/en files. `defaultLang` is what a first-time visitor sees: the store
 * uses Spanish; the admin stays in English until its own texts are translated.
 */
export function provideI18n(options: { defaultLang?: Lang } = {}): EnvironmentProviders {
  const defaultLang = options.defaultLang ?? DEFAULT_LANG;
  registerLocaleData(localeEsHn, 'es-HN');
  return makeEnvironmentProviders([
    provideTransloco({
      config: {
        availableLangs: [...LANGS],
        defaultLang,
        fallbackLang: FALLBACK_LANG,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: BundledLoader,
    }),
    { provide: LOCALE_ID, useValue: LOCALES[defaultLang] },
    provideAppInitializer(() => inject(I18n).init(defaultLang)),
  ]);
}
