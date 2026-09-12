/**
 * Category colour spectrum (DESIGN_LANGUAGE.md → category spectrum). Shared so that a segmented
 * bar and its legend always agree: a colour that appears in a chart must be keyed somewhere, or
 * the segments cannot be mapped back to the categories they represent.
 */
export const CATEGORY_SPECTRUM = [
  'var(--cat-1)',
  'var(--cat-2)',
  'var(--cat-3)',
  'var(--cat-4)',
  'var(--cat-5)',
  'var(--cat-6)',
  'var(--cat-7)',
] as const;

export function spectrumColor(index: number): string {
  return CATEGORY_SPECTRUM[index % CATEGORY_SPECTRUM.length];
}
