<script setup lang="ts">
import Money from '@/components/ui/Money.vue';

withDefaults(
  defineProps<{
    label: string;
    value: number;
    currency?: string;
    caption?: string;
  }>(),
  { currency: 'IDR' },
);
</script>

<template>
  <section class="rounded-card border border-border bg-surface p-6 shadow-card">
    <p class="text-xs font-semibold uppercase tracking-wider text-text-muted">{{ label }}</p>
    <p class="mt-2.5 break-words text-3xl font-bold tracking-tight sm:text-4xl text-text">
      <Money :value="value" :currency="currency" />
    </p>
    <p v-if="caption" class="mt-2 text-xs text-text-muted">{{ caption }}</p>
    <div v-if="$slots.footer" class="mt-4">
      <slot name="footer" />
    </div>
  </section>
</template>
