import { MAX_LINE_QUANTITY, clampQuantity } from '@ch/util/quantity';

describe('clampQuantity', () => {
  it('keeps quantities within 1..max', () => {
    expect(clampQuantity(0)).toBe(1);
    expect(clampQuantity(-4)).toBe(1);
    expect(clampQuantity(5)).toBe(5);
    expect(clampQuantity(MAX_LINE_QUANTITY)).toBe(MAX_LINE_QUANTITY);
    expect(clampQuantity(MAX_LINE_QUANTITY + 1)).toBe(MAX_LINE_QUANTITY);
  });

  it('truncates decimals and rejects non-numbers', () => {
    expect(clampQuantity(2.9)).toBe(2);
    expect(clampQuantity(Number.NaN)).toBe(1);
  });

  it('honours a custom maximum', () => {
    expect(clampQuantity(9, 3)).toBe(3);
  });
});
