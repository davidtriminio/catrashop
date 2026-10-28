/** Mirrors the API default (`Cart:MaxQuantityPerLine`); the server stays the authority and also checks stock. */
export const MAX_LINE_QUANTITY = 20;

/** Keeps a cart line quantity within 1..max. */
export function clampQuantity(quantity: number, max = MAX_LINE_QUANTITY): number {
  if (!Number.isFinite(quantity)) return 1;
  return Math.min(max, Math.max(1, Math.trunc(quantity)));
}
