import { describe, it, expect } from 'vitest';
import { parseMoneyInput, formatThousands } from './moneyParser';

describe('moneyParser', () => {
  it('parses raw numbers and strings', () => {
    expect(parseMoneyInput(50000)).toBe(50000);
    expect(parseMoneyInput('50000')).toBe(50000);
    expect(parseMoneyInput('0')).toBe(0);
    expect(parseMoneyInput('')).toBe(0);
  });

  it('parses numbers with dots and commas', () => {
    expect(parseMoneyInput('50.000')).toBe(50000);
    expect(parseMoneyInput('1.500.000')).toBe(1500000);
    expect(parseMoneyInput('50,000')).toBe(50000);
    expect(parseMoneyInput('1,500,000')).toBe(1500000);
  });

  it('parses thousand shortcuts (k, rb, ribu)', () => {
    expect(parseMoneyInput('50k')).toBe(50000);
    expect(parseMoneyInput('50K')).toBe(50000);
    expect(parseMoneyInput('50rb')).toBe(50000);
    expect(parseMoneyInput('50 ribu')).toBe(50000);
    expect(parseMoneyInput('12.5k')).toBe(12500);
    expect(parseMoneyInput('12,5k')).toBe(12500);
  });

  it('parses million shortcuts (jt, m, juta)', () => {
    expect(parseMoneyInput('1jt')).toBe(1000000);
    expect(parseMoneyInput('1.5jt')).toBe(1500000);
    expect(parseMoneyInput('1,5jt')).toBe(1500000);
    expect(parseMoneyInput('2.5m')).toBe(2500000);
    expect(parseMoneyInput('2 juta')).toBe(2000000);
  });

  it('evaluates inline arithmetic expressions', () => {
    expect(parseMoneyInput('25000 + 15000')).toBe(40000);
    expect(parseMoneyInput('25k + 15k')).toBe(40000);
    expect(parseMoneyInput('100k - 25k')).toBe(75000);
    expect(parseMoneyInput('1.5jt + 500k')).toBe(2000000);
  });

  it('formats numbers with thousand dots', () => {
    expect(formatThousands(50000)).toBe('50.000');
    expect(formatThousands(1500000)).toBe('1.500.000');
    expect(formatThousands(0)).toBe('');
    expect(formatThousands(null)).toBe('');
  });
});
