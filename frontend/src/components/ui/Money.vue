<script setup lang="ts">
import { computed } from 'vue';
import { formatMoney } from '@/lib/format';

// One money format app-wide: id-ID with negatives in parentheses (design language). Negatives are
// always red; positives are neutral unless `colored` marks a semantic gain (income/net), then green.
const props = withDefaults(
  defineProps<{
    value: number;
    currency?: string;
    colored?: boolean;
    fractionDigits?: 0 | 2;
  }>(),
  { currency: 'IDR', colored: false, fractionDigits: 0 },
);

const text = computed(() => formatMoney(props.value, props.currency, props.fractionDigits));

const colorClass = computed(() => {
  if (props.value < 0) return 'text-negative';
  if (props.value > 0 && props.colored) return 'text-positive';
  return '';
});
</script>

<template>
  <span class="tnum" :class="colorClass">{{ text }}</span>
</template>
