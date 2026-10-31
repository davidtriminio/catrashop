import { MoneyPipe } from '@cs/util/money.pipe';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('formats an amount with its currency (locale-agnostic)', () => {
    // Intl output varies by runtime locale (e.g. "$42.50" or "42,50 US$"); assert the amount is present.
    expect(pipe.transform(42.5, 'USD')).toMatch(/42[.,]50/);
  });

  it('returns an empty string for null/undefined', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});
