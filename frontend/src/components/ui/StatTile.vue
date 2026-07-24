<script setup lang="ts">
import Money from '@/components/ui/Money.vue';

withDefaults(
  defineProps<{
    label: string;
    value: number;
    currency?: string;
    /** Color the number by sign (income/expense/net). */
    colored?: boolean;
  }>(),
  { currency: 'IDR', colored: false },
);
</script>

<template>
  <div class="rounded-card border border-border bg-surface p-5">
    <p class="text-[13px] font-medium text-text-muted">{{ label }}</p>
    <p class="mt-2 text-2xl font-semibold">
      <Money :value="value" :currency="currency" :colored="colored" />
    </p>
  </div>
</template>
