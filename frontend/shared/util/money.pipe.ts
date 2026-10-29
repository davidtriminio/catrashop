import { Pipe, PipeTransform } from '@angular/core';
import { CURRENCY_PREFIX, STORE_CURRENCY, STORE_LOCALE } from './store';

/** Formats an amount for display, e.g. money(1234.5, 'HNL') → "LPS. 1,234.50". Defaults to the store currency. */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined, currency: string | null | undefined = STORE_CURRENCY): string {
    if (value === null || value === undefined) return '';
    const code = (currency || STORE_CURRENCY).toUpperCase();
    const prefix = CURRENCY_PREFIX[code];
    try {
      if (prefix) {
        const digits = new Intl.NumberFormat(STORE_LOCALE, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Math.abs(value));
        return `${value < 0 ? '-' : ''}${prefix} ${digits}`;
      }
      return new Intl.NumberFormat(STORE_LOCALE, { style: 'currency', currency: code }).format(value);
    } catch {
      return `${value.toFixed(2)} ${code}`;
    }
  }
}
