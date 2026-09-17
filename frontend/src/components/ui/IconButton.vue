<script setup lang="ts">
import type { Component } from 'vue';

// An icon-only button that is always labelled: a styled tooltip on hover/focus plus an aria-label for
// screen readers. Use this for every icon-only action so its meaning is never ambiguous.
withDefaults(
  defineProps<{
    icon: Component;
    label: string;
    variant?: 'ghost' | 'outline' | 'surface';
    placement?: 'top' | 'bottom';
    active?: boolean;
    danger?: boolean;
    size?: number;
    disabled?: boolean;
  }>(),
  { variant: 'ghost', placement: 'bottom', active: false, danger: false, size: 16, disabled: false },
);

defineEmits<{ click: [] }>();
</script>

<template>
  <span class="group/tt relative inline-flex">
    <button
      type="button"
      class="inline-flex items-center justify-center transition-all duration-150 disabled:cursor-not-allowed disabled:opacity-40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
      :class="[
        variant === 'outline'
          ? [
              'h-9 w-9 rounded-control border shadow-xs',
              active
                ? 'border-accent/40 bg-accent-soft text-accent font-semibold'
                : danger
                  ? 'border-border bg-surface text-text-muted hover:border-negative/30 hover:bg-negative-soft hover:text-negative'
                  : 'border-border bg-surface text-text-muted hover:border-border-strong hover:bg-surface-2 hover:text-text',
            ]
          : variant === 'surface'
            ? [
                'h-9 w-9 rounded-control bg-surface-2 shadow-xs',
                active
                  ? 'bg-accent text-accent-contrast shadow-sm'
                  : danger
                    ? 'text-text-muted hover:bg-negative-soft hover:text-negative'
                    : 'text-text-muted hover:bg-surface-3 hover:text-text',
              ]
            : [
                'rounded-control p-1.5',
                danger
                  ? 'text-text-muted hover:bg-negative-soft hover:text-negative'
                  : active
                    ? 'bg-accent-soft text-accent'
                    : 'text-text-muted hover:bg-surface-2 hover:text-text',
              ],
      ]"
      :aria-label="label"
      :aria-pressed="active"
      :disabled="disabled"
      @click="$emit('click')"
    >
      <component :is="icon" :size="size" :stroke-width="1.8" />
    </button>
    <span
      class="pointer-events-none absolute left-1/2 z-50 -translate-x-1/2 whitespace-nowrap rounded-md border border-border bg-surface px-2.5 py-1 text-xs font-medium text-text opacity-0 shadow-popover transition-all duration-150 group-hover/tt:opacity-100 group-focus-within/tt:opacity-100"
      :class="placement === 'bottom' ? 'top-full mt-2' : 'bottom-full mb-2'"
      role="tooltip"
    >
      {{ label }}
    </span>
  </span>
</template>
