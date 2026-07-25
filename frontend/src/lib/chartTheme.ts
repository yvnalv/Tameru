// Chart colors mirror the design tokens (docs/frontend/DESIGN_LANGUAGE.md). ECharts can't read CSS
// variables at runtime, so the token hexes are duplicated here as the single chart source.
export const chart = {
  positive: '#35D07A',
  negative: '#FF5B60',
  accent: '#35D07A',
  text: '#F4F5F6',
  textMuted: '#8A9097',
  border: '#2B2E31',
  surface: '#1C1D1F',
  surface2: '#26282B',
  // Category spectrum for donut/segmented series.
  spectrum: ['#35D07A', '#9BE15D', '#FFC531', '#FF8A34', '#FF5B60', '#4C9AFF', '#B06BFF'],
};

/** Shared dark tooltip style for ECharts options. */
export const darkTooltip = {
  backgroundColor: chart.surface,
  borderColor: chart.border,
  borderWidth: 1,
  textStyle: { color: chart.text, fontSize: 12 },
  extraCssText: 'border-radius:10px;box-shadow:0 1px 2px rgba(0,0,0,.4);',
};
