/**
 * Money input parser supporting IDR denomination shortcuts and inline arithmetic.
 * Examples:
 *   '50k' or '50rb' -> 50,000
 *   '1.5jt' or '1,5jt' or '1.5m' -> 1,500,000
 *   '25000 + 15000' or '25k + 15k' -> 40,000
 *   '50.000' -> 50,000
 */

export function parseSingleToken(token: string): number {
  const trimmed = token.trim().toLowerCase();
  if (!trimmed) return 0;

  // Check for suffixes
  // Millions: jt, juta, m, mill
  const millionMatch = trimmed.match(/^([0-9]+(?:[.,][0-9]+)?)\s*(?:jt|juta|m|mill)$/);
  if (millionMatch) {
    const num = parseFloat(millionMatch[1].replace(',', '.'));
    return isNaN(num) ? 0 : Math.round(num * 1_000_000);
  }

  // Thousands: k, rb, ribu
  const thousandMatch = trimmed.match(/^([0-9]+(?:[.,][0-9]+)?)\s*(?:k|rb|ribu)$/);
  if (thousandMatch) {
    const num = parseFloat(thousandMatch[1].replace(',', '.'));
    return isNaN(num) ? 0 : Math.round(num * 1_000);
  }

  // Raw number with dots or commas as thousand separators
  let cleaned = trimmed.replace(/\s+/g, '');

  if (cleaned.includes('.') && cleaned.includes(',')) {
    if (cleaned.lastIndexOf(',') > cleaned.lastIndexOf('.')) {
      cleaned = cleaned.replace(/\./g, '').replace(',', '.');
    } else {
      cleaned = cleaned.replace(/,/g, '');
    }
  } else if (cleaned.includes('.')) {
    const parts = cleaned.split('.');
    if (parts.length > 2) {
      cleaned = cleaned.replace(/\./g, '');
    } else if (parts.length === 2) {
      if (parts[1].length === 3) {
        cleaned = parts[0] + parts[1];
      } else {
        cleaned = parts[0] + '.' + parts[1];
      }
    }
  } else if (cleaned.includes(',')) {
    const parts = cleaned.split(',');
    if (parts.length > 2) {
      cleaned = cleaned.replace(/,/g, '');
    } else if (parts.length === 2) {
      if (parts[1].length === 3) {
        cleaned = parts[0] + parts[1];
      } else {
        cleaned = parts[0] + '.' + parts[1];
      }
    }
  }

  const result = parseFloat(cleaned);
  return isNaN(result) ? 0 : Math.round(result);
}

export function parseMoneyInput(raw: string | number | null | undefined): number {
  if (raw === null || raw === undefined) return 0;
  if (typeof raw === 'number') return isNaN(raw) ? 0 : Math.round(raw);

  const str = String(raw).trim();
  if (!str) return 0;

  // If there are addition or subtraction signs, evaluate the expression safely
  if (str.includes('+') || (str.includes('-') && !str.startsWith('-'))) {
    const tokens = str.split(/([+-])/);
    let total = 0;
    let currentOp = '+';

    for (const token of tokens) {
      const t = token.trim();
      if (t === '+' || t === '-') {
        currentOp = t;
      } else if (t) {
        const val = parseSingleToken(t);
        if (currentOp === '+') {
          total += val;
        } else {
          total -= val;
        }
      }
    }
    return Math.max(0, Math.round(total));
  }

  return Math.max(0, parseSingleToken(str));
}

/**
 * Format an integer number with Indonesian thousand dots.
 * Example: 1500000 -> "1.500.000"
 */
export function formatThousands(val: number | null | undefined): string {
  if (val === null || val === undefined || isNaN(val) || val === 0) return '';
  return Math.round(val).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
}
