import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n, LOCALES } from '../i18n/i18n';

export type DateStyle = 'date' | 'datetime' | 'long';

const OPTIONS: Readonly<Record<DateStyle, Intl.DateTimeFormatOptions>> = {
  date: { dateStyle: 'medium' },
  datetime: { dateStyle: 'medium', timeStyle: 'short' },
  long: { dateStyle: 'long', timeStyle: 'short' },
};

/**
 * Formats a date in the active language, e.g. `{{ x | localDate: 'datetime' }}`. Impure on purpose: it re-reads the
 * language signal so dates change as soon as the visitor switches language.
 */
@Pipe({ name: 'localDate', pure: false })
export class LocalDatePipe implements PipeTransform {
  private readonly i18n = inject(I18n);
  private readonly formats = new Map<string, Intl.DateTimeFormat>();

  transform(value: string | Date | null | undefined, style: DateStyle = 'datetime'): string {
    if (!value) return '';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const locale = LOCALES[this.i18n.lang() as keyof typeof LOCALES] ?? LOCALES.es;
    const key = `${locale}|${style}`;
    let format = this.formats.get(key);
    if (!format) {
      format = new Intl.DateTimeFormat(locale, OPTIONS[style]);
      this.formats.set(key, format);
    }
    return format.format(date);
  }
}
