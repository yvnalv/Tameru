<script setup lang="ts">
import { computed } from 'vue';
import '@/lib/echarts';
import VChart from 'vue-echarts';
import { useI18n } from 'vue-i18n';
import type { MonthlyCashflow } from '@/types/api';
import { formatMoney } from '@/lib/format';
import { chart, darkTooltip } from '@/lib/chartTheme';
import { useUiStore } from '@/stores/ui';

// 12-month income vs expense bars (ECharts, dark theme, solid fills — no gradient).
const props = defineProps<{ months: MonthlyCashflow[]; currency?: string }>();
const { t, locale } = useI18n();
const ui = useUiStore();

const monthLabel = (m: number) => new Date(2020, m - 1, 1).toLocaleString(locale.value, { month: 'short' });
const compact = (v: number) => new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format(v);

const option = computed(() => ({
  tooltip: {
    trigger: 'axis',
    ...darkTooltip,
    valueFormatter: (v: number) => formatMoney(v, props.currency ?? 'IDR'),
  },
  grid: { left: 8, right: 8, top: 10, bottom: 4, containLabel: true },
  xAxis: {
    type: 'category',
    data: props.months.map((m) => monthLabel(m.month)),
    axisLabel: { color: chart.textMuted, fontSize: 11 },
    axisLine: { lineStyle: { color: chart.border } },
    axisTick: { show: false },
  },
  yAxis: {
    type: 'value',
    axisLabel: { color: chart.textMuted, fontSize: 11, formatter: (v: number) => compact(v) },
    splitLine: { lineStyle: { color: chart.border, type: 'dashed' } },
  },
  series: [
    {
      name: t('enums.transactionType.Income'),
      type: 'bar',
      data: props.months.map((m) => m.income),
      itemStyle: { color: chart.positive, borderRadius: [3, 3, 0, 0] },
      barMaxWidth: 14,
    },
    {
      name: t('enums.transactionType.Expense'),
      type: 'bar',
      data: props.months.map((m) => m.expense),
      itemStyle: { color: chart.negative, borderRadius: [3, 3, 0, 0] },
      barMaxWidth: 14,
    },
  ],
}));
</script>

<template>
  <div>
    <div class="mb-3 flex items-center gap-4 text-xs text-text-muted">
      <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-sm" style="background: var(--positive)" />{{ t('enums.transactionType.Income') }}</span>
      <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-sm" style="background: var(--negative)" />{{ t('enums.transactionType.Expense') }}</span>
    </div>
    <VChart
      :option="option"
      autoresize
      class="h-56 w-full transition"
      :class="{ 'pointer-events-none blur-md': ui.amountsHidden }"
    />
  </div>
</template>
