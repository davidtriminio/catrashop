/** The store sells in Honduran lempiras; keep in sync with the API's `Store:Currency` setting. */
export const STORE_CURRENCY = 'HNL';

/** Locale used for number formatting (decimal point, thousands comma). */
export const STORE_LOCALE = 'es-HN';

/** How a currency is written in front of an amount. Lempiras read "LPS. 1,234.50"; others fall back to Intl. */
export const CURRENCY_PREFIX: Readonly<Record<string, string>> = { HNL: 'LPS.' };
