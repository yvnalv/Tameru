// Minimal RFC-4180 CSV parser: handles quoted fields, escaped quotes (""), commas and newlines
// inside quotes, and CRLF/LF line endings. Returns records keyed by lowercased, trimmed headers.

export interface ParsedCsv {
  headers: string[];
  records: Record<string, string>[];
}

function splitRows(text: string): string[][] {
  const rows: string[][] = [];
  let field = '';
  let row: string[] = [];
  let inQuotes = false;
  // Strip a leading UTF-8 BOM if present.
  const s = text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;

  for (let i = 0; i < s.length; i++) {
    const c = s[i];
    if (inQuotes) {
      if (c === '"') {
        if (s[i + 1] === '"') { field += '"'; i++; }
        else inQuotes = false;
      } else {
        field += c;
      }
    } else if (c === '"') {
      inQuotes = true;
    } else if (c === ',') {
      row.push(field); field = '';
    } else if (c === '\n') {
      row.push(field); field = '';
      rows.push(row); row = [];
    } else if (c === '\r') {
      // handled by the \n branch; ignore lone CR
    } else {
      field += c;
    }
  }
  // Flush the last field/row if the file doesn't end with a newline.
  if (field.length > 0 || row.length > 0) {
    row.push(field);
    rows.push(row);
  }
  return rows;
}

export function parseCsv(text: string): ParsedCsv {
  const rows = splitRows(text).filter((r) => r.some((c) => c.trim() !== ''));
  if (rows.length === 0) return { headers: [], records: [] };

  const headers = rows[0].map((h) => h.trim());
  const keys = headers.map((h) => h.toLowerCase());
  const records = rows.slice(1).map((cells) => {
    const record: Record<string, string> = {};
    keys.forEach((key, i) => {
      record[key] = (cells[i] ?? '').trim();
    });
    return record;
  });
  return { headers, records };
}
