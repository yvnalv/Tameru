<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'ghost' | 'danger';
    type?: 'button' | 'submit';
    disabled?: boolean;
    loading?: boolean;
    block?: boolean;
  }>(),
  { variant: 'primary', type: 'button', disabled: false, loading: false, block: false },
);

const base =
  'inline-flex items-center justify-center gap-2 rounded-control px-4 h-10 text-sm font-medium whitespace-nowrap shrink-0 ' +
  'transition-colors duration-150 disabled:opacity-50 disabled:cursor-not-allowed select-none';

const variants: Record<string, string> = {
  primary: 'bg-accent text-accent-contrast font-semibold shadow-sm hover:opacity-90 active:opacity-95',
  secondary: 'border border-border bg-surface text-text hover:bg-surface-2 font-medium',
  ghost: 'text-text-muted hover:text-text hover:bg-surface-2 font-medium',
  danger: 'bg-negative text-negative-contrast font-semibold shadow-sm hover:opacity-90',
};

const classes = computed(() => [base, variants[props.variant], props.block ? 'w-full' : '']);
</script>

<template>
  <button :type="type" :class="classes" :disabled="disabled || loading">
    <span
      v-if="loading"
      class="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
      aria-hidden="true"
    />
    <slot />
  </button>
</template>
