// Chart colors mirror the design tokens for both Light and Dark themes.
// ECharts cannot read CSS variables directly at runtime, so hexes are resolved dynamically.

export function getChartTheme(isDark: boolean) {
  if (isDark) {
    return {
      positive: '#35D07A',
      negative: '#FF5B60',
      warning: '#FFC531',
      accent: '#5558F0',
      text: '#F8FAFC',
      textMuted: '#94A3B8',
      border: '#222631',
      surface: '#14171F',
      surface2: '#1D222D',
      spectrum: ['#5558F0', '#35D07A', '#FFC531', '#FF8A34', '#FF5B60', '#4C9AFF', '#B06BFF'],
      tooltip: {
        backgroundColor: '#14171F',
        borderColor: '#222631',
        borderWidth: 1,
        textStyle: { color: '#F8FAFC', fontSize: 12 },
        extraCssText: 'border-radius:10px;box-shadow:0 4px 14px rgba(0,0,0,0.5);',
      },
    };
  }
  return {
    positive: '#047857',
    negative: '#DC2626',
    warning: '#D97706',
    accent: '#3B46F1',
    text: '#111827',
    textMuted: '#64748B',
    border: '#EAEBF0',
    surface: '#FFFFFF',
    surface2: '#EEF0F6',
    spectrum: ['#3B46F1', '#10B981', '#F59E0B', '#F97316', '#EF4444', '#06B6D4', '#8B5CF6'],
    tooltip: {
      backgroundColor: '#FFFFFF',
      borderColor: '#EAEBF0',
      borderWidth: 1,
      textStyle: { color: '#111827', fontSize: 12 },
      extraCssText: 'border-radius:10px;box-shadow:0 4px 14px rgba(0,0,0,0.08);',
    },
  };
}

// Backward-compatible fallback
export const chart = getChartTheme(true);
export const darkTooltip = chart.tooltip;

