import { Pipe, PipeTransform } from '@angular/core';

/** Formats an amount with its ISO currency code, e.g. money(42.5, 'USD') → "$42.50". */
@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined, currency = 'USD'): string {
    if (value === null || value === undefined) return '';
    try {
      return new Intl.NumberFormat(undefined, { style: 'currency', currency }).format(value);
    } catch {
      return `${value.toFixed(2)} ${currency}`;
    }
  }
}
