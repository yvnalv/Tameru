<script setup lang="ts">
import { computed } from 'vue';
import '@/lib/echarts';
import VChart from 'vue-echarts';
import { formatMoney } from '@/lib/format';
import { getChartTheme } from '@/lib/chartTheme';
import { useThemeStore } from '@/stores/theme';
import { useUiStore } from '@/stores/ui';
import type { DayOfWeekItemDto } from '@/types/api';

const props = withDefaults(
  defineProps<{
    data: DayOfWeekItemDto[];
    currency?: string;
  }>(),
  { currency: 'IDR' },
);

const ui = useUiStore();
const themeStore = useThemeStore();

const shortDayLabels: Record<string, string> = {
  Monday: 'Mon',
  Tuesday: 'Tue',
  Wednesday: 'Wed',
  Thursday: 'Thu',
  Friday: 'Fri',
  Saturday: 'Sat',
  Sunday: 'Sun',
};

const option = computed(() => {
  const ct = getChartTheme(themeStore.isDark);
  const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  return {
    grid: {
      left: 10,
      right: 10,
      top: 20,
      bottom: 25,
      containLabel: true,
    },
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      ...ct.tooltip,
      formatter: (params: any) => {
        if (!params || !params.length) return '';
        const p = params[0];
        const day = days[p.dataIndex];
        const item = props.data.find((d) => d.dayOfWeek.toLowerCase() === day.toLowerCase());
        const total = item ? item.totalAmount : 0;
        const avg = p.value;
        return `
          <div class="font-semibold text-xs mb-1">${day}</div>
          <div class="text-[11px] text-text-muted flex justify-between gap-3">
            <span>Average / day:</span>
            <b class="tnum text-text">${ui.amountsHidden ? '••••••' : formatMoney(avg, props.currency)}</b>
          </div>
          <div class="text-[11px] text-text-muted flex justify-between gap-3 mt-0.5">
            <span>Period Total:</span>
            <span class="tnum">${ui.amountsHidden ? '••••••' : formatMoney(total, props.currency)}</span>
          </div>
        `;
      },
    },
    xAxis: {
      type: 'category',
      data: days.map((d) => shortDayLabels[d] || d),
      axisLine: { lineStyle: { color: ct.border } },
      axisTick: { show: false },
      axisLabel: {
        color: ct.textMuted,
        fontSize: 11,
        fontWeight: 600,
      },
    },
    yAxis: {
      type: 'value',
      splitLine: { lineStyle: { color: ct.border, type: 'dashed' } },
      axisLabel: {
        color: ct.textMuted,
        fontSize: 10,
        formatter: (v: number) => {
          if (ui.amountsHidden) return '•••';
          if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`;
          if (v >= 1_000) return `${Math.round(v / 1_000)}k`;
          return v.toString();
        },
      },
    },
    series: [
      {
        type: 'bar',
        barWidth: '45%',
        borderRadius: [4, 4, 0, 0],
        data: days.map((day) => {
          const isWeekend = day === 'Saturday' || day === 'Sunday';
          const item = props.data.find((d) => d.dayOfWeek.toLowerCase() === day.toLowerCase());
          const val = item ? item.averagePerDay : 0;
          return {
            value: val,
            itemStyle: {
              color: isWeekend ? '#35D07A' : (themeStore.isDark ? '#475569' : '#94A3B8'),
            },
          };
        }),
      },
    ],
  };
});
</script>

<template>
  <div class="relative w-full">
    <VChart
      :option="option"
      autoresize
      class="h-64 w-full transition"
      :class="{ 'pointer-events-none blur-md': ui.amountsHidden }"
    />
  </div>
</template>
