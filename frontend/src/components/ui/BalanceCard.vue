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
  <section class="rounded-hero border border-border bg-surface p-6 shadow-lift">
    <p class="text-sm font-medium text-text-muted">{{ label }}</p>
    <p class="mt-3 break-words text-3xl font-semibold tracking-tight sm:text-4xl">
      <Money :value="value" :currency="currency" />
    </p>
    <p v-if="caption" class="mt-2 text-[13px] text-text-muted">{{ caption }}</p>
    <div v-if="$slots.footer" class="mt-4">
      <slot name="footer" />
    </div>
  </section>
</template>
