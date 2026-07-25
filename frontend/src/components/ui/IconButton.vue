<script setup lang="ts">
import type { Component } from 'vue';

// An icon-only button that is always labelled: a styled tooltip on hover/focus plus an aria-label for
// screen readers. Use this for every icon-only action so its meaning is never ambiguous.
withDefaults(
  defineProps<{
    icon: Component;
    label: string;
    danger?: boolean;
    size?: number;
    disabled?: boolean;
  }>(),
  { danger: false, size: 16, disabled: false },
);

defineEmits<{ click: [] }>();
</script>

<template>
  <span class="group/tt relative inline-flex">
    <button
      type="button"
      class="inline-flex items-center justify-center rounded-control p-1.5 text-text-muted transition-colors hover:bg-surface-2 disabled:opacity-40"
      :class="danger ? 'hover:text-negative' : 'hover:text-text'"
      :aria-label="label"
      :disabled="disabled"
      @click="$emit('click')"
    >
      <component :is="icon" :size="size" :stroke-width="1.75" />
    </button>
    <span
      class="pointer-events-none absolute bottom-full left-1/2 z-50 mb-1.5 -translate-x-1/2 whitespace-nowrap rounded-md border border-border bg-surface-2 px-2 py-1 text-xs font-medium text-text opacity-0 shadow-lift transition-opacity duration-100 group-hover/tt:opacity-100 group-focus-within/tt:opacity-100"
      role="tooltip"
    >
      {{ label }}
    </span>
  </span>
</template>
