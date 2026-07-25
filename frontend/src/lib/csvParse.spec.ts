import { describe, it, expect } from 'vitest';
import { parseCsv } from '@/lib/csvParse';

describe('parseCsv', () => {
  it('parses headers and records keyed by lowercased header', () => {
    const { headers, records } = parseCsv('Name,Amount\nBCA,1000\nCash,500');
    expect(headers).toEqual(['Name', 'Amount']);
    expect(records).toEqual([
      { name: 'BCA', amount: '1000' },
      { name: 'Cash', amount: '500' },
    ]);
  });

  it('handles quoted fields with commas and escaped quotes', () => {
    const { records } = parseCsv('title,note\n"Lunch, big","She said ""hi"""');
    expect(records[0]).toEqual({ title: 'Lunch, big', note: 'She said "hi"' });
  });

  it('handles newlines inside quotes and CRLF endings', () => {
    const { records } = parseCsv('a,b\r\n"line1\nline2",x\r\n');
    expect(records).toHaveLength(1);
    expect(records[0].a).toBe('line1\nline2');
    expect(records[0].b).toBe('x');
  });

  it('strips a leading BOM and skips blank lines', () => {
    const { records } = parseCsv('﻿name\nBCA\n\n');
    expect(records).toEqual([{ name: 'BCA' }]);
  });

  it('returns empty for empty input', () => {
    expect(parseCsv('')).toEqual({ headers: [], records: [] });
  });
});
