import { describe, it, expect } from 'vitest';
import { contrastRatio, passesWcagAA } from './contrast';

describe('Design System Contrast Ratios (WCAG 2.2 AA Compliance)', () => {
  describe('Light Mode Tokens', () => {
    const lightSurface = '#ffffff';
    const lightCanvas = '#f5f6fa';
    const lightText = '#111827';
    const lightTextMuted = '#64748b';
    const lightAccent = '#3b46f1';
    const lightAccentContrast = '#ffffff';
    const lightPositive = '#047857';
    const lightPositiveContrast = '#ffffff';
    const lightNegative = '#dc2626';
    const lightNegativeContrast = '#ffffff';
    const lightBorderStrong = '#718096';

    it('Primary text on card surface exceeds 4.5:1 (passes AA & AAA)', () => {
      const ratio = contrastRatio(lightText, lightSurface);
      expect(ratio).toBeGreaterThanOrEqual(7.0);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Primary text on canvas background exceeds 4.5:1', () => {
      const ratio = contrastRatio(lightText, lightCanvas);
      expect(ratio).toBeGreaterThanOrEqual(7.0);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Muted text on card surface satisfies 4.5:1', () => {
      const ratio = contrastRatio(lightTextMuted, lightSurface);
      expect(ratio).toBeGreaterThanOrEqual(4.5);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Accent button text on accent fill satisfies 4.5:1', () => {
      const ratio = contrastRatio(lightAccentContrast, lightAccent);
      expect(ratio).toBeGreaterThanOrEqual(4.5);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Interactive input borders satisfy 3:1 non-text contrast against surface', () => {
      const ratio = contrastRatio(lightBorderStrong, lightSurface);
      expect(passesWcagAA(ratio, true)).toBe(true);
    });

    it('Positive fill and negative danger button texts satisfy 4.5:1', () => {
      expect(passesWcagAA(contrastRatio(lightPositiveContrast, lightPositive))).toBe(true);
      expect(passesWcagAA(contrastRatio(lightNegativeContrast, lightNegative))).toBe(true);
    });
  });

  describe('Dark Mode Tokens', () => {
    const darkSurface = '#14171f';
    const darkCanvas = '#0b0d11';
    const darkText = '#f8fafc';
    const darkTextMuted = '#94a3b8';
    const darkAccent = '#5558f0';
    const darkAccentContrast = '#ffffff';
    const darkPositive = '#35d07a';
    const darkPositiveContrast = '#0b0f0c';
    const darkNegative = '#ff5b60';
    const darkNegativeContrast = '#0b0f0c';
    const darkBorderStrong = '#64748b';

    it('Primary text on card surface exceeds 4.5:1', () => {
      const ratio = contrastRatio(darkText, darkSurface);
      expect(ratio).toBeGreaterThanOrEqual(7.0);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Primary text on canvas background exceeds 4.5:1', () => {
      const ratio = contrastRatio(darkText, darkCanvas);
      expect(ratio).toBeGreaterThanOrEqual(7.0);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Muted text on card surface satisfies 4.5:1', () => {
      const ratio = contrastRatio(darkTextMuted, darkSurface);
      expect(ratio).toBeGreaterThanOrEqual(4.5);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Accent button text on accent fill satisfies 4.5:1', () => {
      const ratio = contrastRatio(darkAccentContrast, darkAccent);
      expect(ratio).toBeGreaterThanOrEqual(4.5);
      expect(passesWcagAA(ratio)).toBe(true);
    });

    it('Interactive input borders satisfy 3:1 non-text contrast against surface', () => {
      const ratio = contrastRatio(darkBorderStrong, darkSurface);
      expect(passesWcagAA(ratio, true)).toBe(true);
    });

    it('Positive fill and negative danger button texts satisfy 4.5:1', () => {
      expect(passesWcagAA(contrastRatio(darkPositiveContrast, darkPositive))).toBe(true);
      expect(passesWcagAA(contrastRatio(darkNegativeContrast, darkNegative))).toBe(true);
    });
  });
});
