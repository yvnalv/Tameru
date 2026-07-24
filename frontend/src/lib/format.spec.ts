import { describe, it, expect } from 'vitest';
import { formatMoney, formatNumber, formatSignedMoney } from '@/lib/format';

describe('formatNumber (id-ID)', () => {
  it('groups thousands with dots', () => {
    expect(formatNumber(1234567)).toBe('1.234.567');
  });

  it('wraps negatives in parentheses', () => {
    expect(formatNumber(-1234567)).toBe('(1.234.567)');
  });

  it('renders two fraction digits with a comma decimal', () => {
    expect(formatNumber(1234.56, 2)).toBe('1.234,56');
    expect(formatNumber(-1234.56, 2)).toBe('(1.234,56)');
  });

  it('formats zero without a sign', () => {
    expect(formatNumber(0)).toBe('0');
  });
});

describe('formatMoney', () => {
  it('prefixes Rp for IDR', () => {
    expect(formatMoney(5000000)).toBe('Rp 5.000.000');
  });

  it('wraps negative money in parentheses', () => {
    expect(formatMoney(-250000)).toBe('(Rp 250.000)');
  });

  it('uses the raw code for non-IDR currencies', () => {
    expect(formatMoney(1000, 'USD')).toBe('USD 1.000');
  });
});

describe('formatSignedMoney', () => {
  it('shows a plus for income', () => {
    expect(formatSignedMoney(3000000)).toBe('+Rp 3.000.000');
  });

  it('shows a true minus sign for expense', () => {
    expect(formatSignedMoney(-500000)).toBe('−Rp 500.000');
  });

  it('has no sign for zero', () => {
    expect(formatSignedMoney(0)).toBe('Rp 0');
  });
});
