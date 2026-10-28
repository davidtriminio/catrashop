import { STORE_CURRENCY, STORE_LOCALE } from './store';
import { Pipe, PipeTransform } from '@angular/core';

/** Formats an amount in the store locale, e.g. money(1234.5, 'HNL') → "L 1,234.50". */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined, currency: string | null | undefined = STORE_CURRENCY): string {
    if (value === null || value === undefined) return '';
    try {
      return new Intl.NumberFormat(STORE_LOCALE, { style: 'currency', currency: currency || STORE_CURRENCY }).format(value);
    } catch {
      return `${value.toFixed(2)} ${currency || STORE_CURRENCY}`;
    }
  }
}
