import { MoneyPipe } from '@cs/util/money.pipe';
import { STORE_CURRENCY } from '@cs/util/store';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('writes lempiras as LPS. with a decimal point and thousands commas', () => {
    expect(pipe.transform(1234.5, 'HNL')).toBe('LPS. 1,234.50');
    expect(pipe.transform(320, 'hnl')).toBe('LPS. 320.00');
    expect(pipe.transform(1000000, 'HNL')).toBe('LPS. 1,000,000.00');
  });

  it('puts the minus sign before the prefix', () => {
    expect(pipe.transform(-45, 'HNL')).toBe('-LPS. 45.00');
  });

  it('defaults to the store currency when none is given', () => {
    expect(STORE_CURRENCY).toBe('HNL');
    expect(pipe.transform(320)).toBe('LPS. 320.00');
    expect(pipe.transform(320, null)).toBe('LPS. 320.00');
  });

  it('keeps other currencies readable and handles missing values', () => {
    expect(pipe.transform(5, 'USD')).toContain('5.00');
    expect(pipe.transform(5, 'USD')).not.toContain('LPS');
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});
