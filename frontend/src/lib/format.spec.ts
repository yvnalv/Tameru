import { describe, it, expect } from 'vitest';
import { formatMoney, formatNumber, formatRowDate } from '@/lib/format';

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

describe('formatRowDate', () => {
  const today = new Date('2026-09-12T00:00:00');

  it('omits the year for a date in the current year', () => {
    expect(formatRowDate('2026-07-26', 'id', today)).not.toContain('2026');
  });

  it('appends the year for a date from another year', () => {
    expect(formatRowDate('2024-07-26', 'id', today)).toContain('2024');
  });

  it('still names the day and month when the year is shown', () => {
    const out = formatRowDate('2024-05-03', 'id', today);
    expect(out).toContain('Mei');
    expect(out).toContain('03');
  });

  it('localises the month name', () => {
    expect(formatRowDate('2026-05-03', 'id', today)).toContain('Mei');
    expect(formatRowDate('2026-05-03', 'en', today)).toContain('May');
  });
});
