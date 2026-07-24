// id-ID number/money formatting (docs/frontend/DESIGN_LANGUAGE.md → Locale/number default).
// Negatives render in parentheses; always pair the output with the `.tnum` (tabular figures) class.

const idID = new Intl.NumberFormat('id-ID', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});

const idID2 = new Intl.NumberFormat('id-ID', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/** Absolute-value formatter, choosing the fraction style. */
function formatAbs(value: number, fractionDigits: 0 | 2): string {
  const abs = Math.abs(value);
  return fractionDigits === 2 ? idID2.format(abs) : idID.format(abs);
}

/**
 * Format a number id-ID with negatives in parentheses, e.g. `1.234.567` / `(1.234,56)`.
 * @param fractionDigits 0 (default) or 2 for values that carry cents.
 */
export function formatNumber(value: number, fractionDigits: 0 | 2 = 0): string {
  const body = formatAbs(value, fractionDigits);
  return value < 0 ? `(${body})` : body;
}

/**
 * Format money as `Rp 1.234.567` (id-ID), negatives as `(Rp 1.234.567)`.
 * IDR is the functional currency; a different code is prefixed as-is (e.g. `USD 1.000`).
 */
export function formatMoney(
  value: number,
  currencyCode = 'IDR',
  fractionDigits: 0 | 2 = 0,
): string {
  const symbol = currencyCode === 'IDR' ? 'Rp' : currencyCode;
  const body = `${symbol} ${formatAbs(value, fractionDigits)}`;
  return value < 0 ? `(${body})` : body;
}

/** Signed money for ledger rows: `+Rp 1.000` / `−Rp 1.000` (true minus sign, not a hyphen). */
export function formatSignedMoney(value: number, currencyCode = 'IDR'): string {
  const body = formatMoney(Math.abs(value), currencyCode);
  if (value === 0) return body;
  return value > 0 ? `+${body}` : `−${body}`;
}
