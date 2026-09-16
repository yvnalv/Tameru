<script setup lang="ts">
import { computed } from 'vue';
import '@/lib/echarts';
import VChart from 'vue-echarts';
import { useI18n } from 'vue-i18n';
import type { MonthlyCashflow } from '@/types/api';
import { formatMoney } from '@/lib/format';
import { chart, darkTooltip } from '@/lib/chartTheme';
import { useUiStore } from '@/stores/ui';

// 12-month income vs expense bars or savings rate trend (ECharts, dark theme, solid fills — no gradient).
const props = withDefaults(
  defineProps<{ months: MonthlyCashflow[]; currency?: string; mode?: 'cashflow' | 'savings' }>(),
  { currency: 'IDR', mode: 'cashflow' },
);
const { t, locale } = useI18n();
const ui = useUiStore();

const monthLabel = (m: number) => new Date(2020, m - 1, 1).toLocaleString(locale.value, { month: 'short' });
const compact = (v: number) => new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format(v);

const option = computed(() => {
  if (props.mode === 'savings') {
    return {
      tooltip: {
        trigger: 'axis',
        ...darkTooltip,
        valueFormatter: (v: number) => `${v.toFixed(1)}%`,
      },
      grid: { left: 8, right: 8, top: 16, bottom: 4, containLabel: true },
      xAxis: {
        type: 'category',
        data: props.months.map((m) => monthLabel(m.month)),
        axisLabel: { color: chart.textMuted, fontSize: 11 },
        axisLine: { lineStyle: { color: chart.border } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        axisLabel: { color: chart.textMuted, fontSize: 11, formatter: '{value}%' },
        splitLine: { lineStyle: { color: chart.border, type: 'dashed' } },
      },
      series: [
        {
          name: t('dashboard.savingsRate'),
          type: 'line',
          data: props.months.map((m) =>
            m.savingsRate ?? (m.income > 0 ? Math.round(((m.income - m.expense) / m.income) * 1000) / 10 : 0),
          ),
          itemStyle: { color: chart.accent },
          lineStyle: { color: chart.accent, width: 2.5 },
          symbol: 'circle',
          symbolSize: 6,
          markLine: {
            silent: true,
            symbol: 'none',
            lineStyle: { color: chart.textMuted, type: 'dashed', width: 1 },
            data: [
              {
                yAxis: 20,
                label: {
                  formatter: '20% Target',
                  position: 'insideEndTop',
                  color: chart.textMuted,
                  fontSize: 10,
                },
              },
            ],
          },
        },
      ],
    };
  }

  return {
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
  };
});
</script>

<template>
  <div>
    <div v-if="mode === 'cashflow'" class="mb-3 flex items-center gap-4 text-xs text-text-muted">
      <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-sm" style="background: var(--positive)" />{{ t('enums.transactionType.Income') }}</span>
      <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-sm" style="background: var(--negative)" />{{ t('enums.transactionType.Expense') }}</span>
    </div>
    <div v-else class="mb-3 flex items-center gap-4 text-xs text-text-muted">
      <span class="inline-flex items-center gap-1.5"><span class="h-2.5 w-2.5 rounded-full" style="background: var(--accent)" />{{ t('dashboard.savingsRate') }}</span>
      <span class="text-[11px] text-text-muted">({{ t('dashboard.savingsTarget') }})</span>
    </div>
    <VChart
      :option="option"
      autoresize
      class="h-56 w-full transition"
      :class="{ 'pointer-events-none blur-md': ui.amountsHidden }"
    />
  </div>
</template>
