<script setup lang="ts">
import { computed } from 'vue';
import '@/lib/echarts';
import VChart from 'vue-echarts';
import { useI18n } from 'vue-i18n';
import type { MonthlyCashflow } from '@/types/api';
import { formatMoney } from '@/lib/format';
import { getChartTheme } from '@/lib/chartTheme';
import { useThemeStore } from '@/stores/theme';
import { useUiStore } from '@/stores/ui';

// 12-month income vs expense bars or savings rate trend with dynamic Light/Dark theme calibration.
const props = withDefaults(
  defineProps<{ months: MonthlyCashflow[]; currency?: string; mode?: 'cashflow' | 'savings' | 'stacked' }>(),
  { currency: 'IDR', mode: 'cashflow' },
);
const { t, locale } = useI18n();
const ui = useUiStore();
const themeStore = useThemeStore();

const monthLabel = (m: number) => new Date(2020, m - 1, 1).toLocaleString(locale.value, { month: 'short' });
const compact = (v: number) => new Intl.NumberFormat(locale.value, { notation: 'compact', maximumFractionDigits: 1 }).format(v);

const option = computed(() => {
  const ct = getChartTheme(themeStore.isDark);

  if (props.mode === 'savings') {
    return {
      tooltip: {
        trigger: 'axis',
        ...ct.tooltip,
        valueFormatter: (v: number) => `${v.toFixed(1)}%`,
      },
      grid: { left: 8, right: 8, top: 16, bottom: 4, containLabel: true },
      xAxis: {
        type: 'category',
        data: props.months.map((m) => monthLabel(m.month)),
        axisLabel: { color: ct.textMuted, fontSize: 11 },
        axisLine: { lineStyle: { color: ct.border } },
        axisTick: { show: false },
      },
      yAxis: {
        type: 'value',
        axisLabel: { color: ct.textMuted, fontSize: 11, formatter: '{value}%' },
        splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
      },
      series: [
        {
          name: t('dashboard.savingsRate'),
          type: 'line',
          smooth: true,
          data: props.months.map((m) =>
            m.savingsRate ?? (m.income > 0 ? Math.round(((m.income - m.expense) / m.income) * 1000) / 10 : 0),
          ),
          itemStyle: { color: ct.accent },
          lineStyle: { color: ct.accent, width: 2.5 },
          symbol: 'circle',
          symbolSize: 6,
          markLine: {
            silent: true,
            symbol: 'none',
            lineStyle: { color: ct.textMuted, type: 'dashed', width: 1 },
            data: [
              {
                yAxis: 20,
                label: {
                  formatter: '20% Target',
                  position: 'insideEndTop',
                  color: ct.textMuted,
                  fontSize: 10,
                },
              },
            ],
          },
        },
      ],
    };
  }

  const isStacked = props.mode === 'stacked';

  return {
    tooltip: {
      trigger: 'axis',
      ...ct.tooltip,
      valueFormatter: (v: number) => (ui.amountsHidden ? '••••••' : formatMoney(v, props.currency ?? 'IDR')),
    },
    grid: { left: 8, right: 8, top: 12, bottom: 4, containLabel: true },
    xAxis: {
      type: 'category',
      data: props.months.map((m) => monthLabel(m.month)),
      axisLabel: { color: ct.textMuted, fontSize: 11 },
      axisLine: { lineStyle: { color: ct.border } },
      axisTick: { show: false },
    },
    yAxis: {
      type: 'value',
      axisLabel: { color: ct.textMuted, fontSize: 11, formatter: (v: number) => compact(v) },
      splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
    },
    series: [
      {
        name: t('enums.transactionType.Income'),
        type: 'bar',
        stack: isStacked ? 'total' : undefined,
        data: props.months.map((m) => m.income),
        itemStyle: { color: ct.positive, borderRadius: isStacked ? [0, 0, 0, 0] : [4, 4, 0, 0] },
        barMaxWidth: isStacked ? 24 : 16,
      },
      {
        name: t('enums.transactionType.Expense'),
        type: 'bar',
        stack: isStacked ? 'total' : undefined,
        data: props.months.map((m) => m.expense),
        itemStyle: { color: ct.negative, borderRadius: [4, 4, 0, 0] },
        barMaxWidth: isStacked ? 24 : 16,
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
