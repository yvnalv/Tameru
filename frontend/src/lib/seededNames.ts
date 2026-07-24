// Localized display for seeded reference data (categories, master-plan sections). Per CLAUDE.md the
// mapping overrides a name only while it is still at its seeded English default; a user rename no
// longer matches a key here and so is shown verbatim.
const MAP: Record<string, { en: string; id: string }> = {
  // Budget-level categories & master-plan sections
  Income: { en: 'Income', id: 'Pemasukan' },
  Investment: { en: 'Investment', id: 'Investasi' },
  Needs: { en: 'Needs', id: 'Kebutuhan' },
  Wants: { en: 'Wants', id: 'Keinginan' },
  // Category-level
  Food: { en: 'Food', id: 'Makanan' },
  Personal: { en: 'Personal', id: 'Pribadi' },
  Saving: { en: 'Saving', id: 'Tabungan' },
  Entertainment: { en: 'Entertainment', id: 'Hiburan' },
  Gold: { en: 'Gold', id: 'Emas' },
  Transportation: { en: 'Transportation', id: 'Transportasi' },
  Internet: { en: 'Internet', id: 'Internet' },
};

export function displayName(name: string | null | undefined, locale: string): string {
  if (!name) return '';
  const entry = MAP[name];
  if (!entry) return name;
  return locale === 'id' ? entry.id : entry.en;
}
