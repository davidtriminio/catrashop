import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n } from '../i18n/i18n';

/**
 * Picks the singular or plural text for a count: `{{ 'cart.items' | plural: n }}` reads `cart.items.one` when n is 1
 * and `cart.items.other` otherwise, with `{{count}}` available. Impure so it follows the active language.
 */
@Pipe({ name: 'plural', pure: false })
export class PluralPipe implements PipeTransform {
  private readonly i18n = inject(I18n);

  transform(key: string, count: number): string {
    this.i18n.lang();
    return this.i18n.t(`${key}.${count === 1 ? 'one' : 'other'}`, { count });
  }
}
