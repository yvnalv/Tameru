import { describe, it, expect } from 'vitest';
import en from '@/i18n/locales/en';
import id from '@/i18n/locales/id';

// The two dictionaries must stay structurally identical (CLAUDE.md → Internationalization): every
// user-facing string exists in both English and Bahasa Indonesia. This guards against drift when
// either file is edited.
type Dict = Record<string, unknown>;

function paths(obj: Dict, prefix = ''): string[] {
  return Object.entries(obj).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    return value && typeof value === 'object'
      ? paths(value as Dict, path)
      : [path];
  });
}

describe('i18n locale parity', () => {
  const enPaths = paths(en as unknown as Dict).sort();
  const idPaths = paths(id as unknown as Dict).sort();

  it('EN and ID have the same set of keys', () => {
    const missingInId = enPaths.filter((p) => !idPaths.includes(p));
    const missingInEn = idPaths.filter((p) => !enPaths.includes(p));
    expect(missingInId, `keys missing in id.ts: ${missingInId.join(', ')}`).toEqual([]);
    expect(missingInEn, `keys missing in en.ts: ${missingInEn.join(', ')}`).toEqual([]);
  });

  it('has no empty string values', () => {
    const empty = (obj: Dict, prefix = ''): string[] =>
      Object.entries(obj).flatMap(([key, value]) => {
        const path = prefix ? `${prefix}.${key}` : key;
        if (value && typeof value === 'object') return empty(value as Dict, path);
        return typeof value === 'string' && value.trim() === '' ? [path] : [];
      });
    expect(empty(en as unknown as Dict)).toEqual([]);
    expect(empty(id as unknown as Dict)).toEqual([]);
  });
});
