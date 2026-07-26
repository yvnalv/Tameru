<script setup lang="ts">
import { computed } from 'vue';
import '@/lib/echarts';
import VChart from 'vue-echarts';
import { formatMoney } from '@/lib/format';
import { chart, darkTooltip } from '@/lib/chartTheme';

// A category-spectrum donut. Data is name+value; colors rotate through the spectrum.
const props = withDefaults(
  defineProps<{ data: { name: string; value: number }[]; currency?: string }>(),
  { currency: 'IDR' },
);

const total = computed(() => props.data.reduce((s, d) => s + d.value, 0));

const option = computed(() => ({
  tooltip: {
    trigger: 'item',
    ...darkTooltip,
    formatter: (p: { name: string; value: number; percent: number }) =>
      `${p.name}<br/><b>${formatMoney(p.value, props.currency)}</b> · ${p.percent}%`,
  },
  series: [
    {
      type: 'pie',
      radius: ['58%', '82%'],
      center: ['50%', '50%'],
      avoidLabelOverlap: false,
      itemStyle: { borderColor: chart.surface, borderWidth: 2 },
      label: { show: false },
      labelLine: { show: false },
      data: props.data.map((d, i) => ({
        name: d.name,
        value: d.value,
        itemStyle: { color: chart.spectrum[i % chart.spectrum.length] },
      })),
    },
  ],
}));
</script>

<template>
  <div class="relative">
    <VChart :option="option" autoresize class="h-52 w-full" />
    <!-- Center total -->
    <div class="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
      <span class="text-[11px] text-text-muted">{{ $t('common.total') }}</span>
      <span class="tnum text-base font-semibold"><slot name="center">{{ formatMoney(total, currency) }}</slot></span>
    </div>
  </div>
</template>
