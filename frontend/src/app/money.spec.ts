import { MoneyPipe } from '@ch/util/money.pipe';
import { STORE_CURRENCY } from '@ch/util/store';

describe('MoneyPipe', () => {
  const pipe = new MoneyPipe();

  it('formats lempiras with the L symbol, a decimal point and thousands commas', () => {
    expect(pipe.transform(1234.5, 'HNL').replace(/\s/g, ' ')).toBe('L 1,234.50');
  });

  it('defaults to the store currency when none is given', () => {
    expect(STORE_CURRENCY).toBe('HNL');
    expect(pipe.transform(320)).toBe(pipe.transform(320, 'HNL'));
    expect(pipe.transform(320, null)).toBe(pipe.transform(320, 'HNL'));
  });

  it('keeps other currencies readable and handles missing values', () => {
    expect(pipe.transform(5, 'USD')).toContain('5.00');
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});
