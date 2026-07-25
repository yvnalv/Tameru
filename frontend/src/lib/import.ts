// Shared types + helpers for the client-side CSV import flow. Import reuses the existing, fully
// validated create endpoints (one request per row), so all money rules still apply server-side.
import { downloadCsv } from '@/lib/csv';

export interface ImportConfig {
  /** File name (without extension) for the downloadable template. */
  templateName: string;
  /** Expected column headers, in order. */
  columns: string[];
  /** One example row aligned to `columns`. */
  sample: string[];
  /** Short human label for a record in the preview (e.g. its title). */
  summary: (record: Record<string, string>) => string;
  /** Return an error message if the record is invalid, else null. */
  validate: (record: Record<string, string>) => string | null;
  /** Create the record via the existing API. Throws ApiClientError on failure. */
  importRecord: (record: Record<string, string>) => Promise<void>;
}

/** Download a header + sample-row CSV template for the given config. */
export function downloadTemplate(config: ImportConfig): void {
  const escape = (v: string) => `"${v.replace(/"/g, '""')}"`;
  const csv = `﻿${config.columns.map(escape).join(',')}\r\n${config.sample.map(escape).join(',')}`;
  downloadCsv(`${config.templateName}.csv`, csv);
}

/** Lenient positive-number parse (strips thousands separators/spaces). */
export function parseAmount(raw: string): number | null {
  if (!raw) return null;
  const n = Number(raw.replace(/[,\s]/g, ''));
  return Number.isFinite(n) ? n : null;
}
