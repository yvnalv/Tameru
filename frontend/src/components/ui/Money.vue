<script setup lang="ts">
import { computed } from 'vue';
import { formatMoney, formatSignedMoney } from '@/lib/format';

const props = withDefaults(
  defineProps<{
    value: number;
    currency?: string;
    /** Show an explicit +/− sign and color by sign (for ledger amounts). */
    signed?: boolean;
    /** Color negatives red / positives green even without a sign. */
    colored?: boolean;
  }>(),
  { currency: 'IDR', signed: false, colored: false },
);

const text = computed(() =>
  props.signed
    ? formatSignedMoney(props.value, props.currency)
    : formatMoney(props.value, props.currency),
);

const colorClass = computed(() => {
  if (!props.signed && !props.colored) return '';
  if (props.value > 0) return 'text-positive';
  if (props.value < 0) return 'text-negative';
  return '';
});
</script>

<template>
  <span class="tnum" :class="colorClass">{{ text }}</span>
</template>
