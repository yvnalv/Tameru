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
  'inline-flex items-center justify-center gap-2 rounded-control px-4 h-10 text-sm font-medium ' +
  'transition-colors duration-150 disabled:opacity-50 disabled:cursor-not-allowed select-none';

const variants: Record<string, string> = {
  primary: 'bg-accent text-accent-contrast hover:bg-accent-hover active:bg-accent-active',
  secondary: 'border border-border text-text hover:bg-surface-2',
  ghost: 'text-text-muted hover:text-text hover:bg-surface-2',
  danger: 'bg-negative text-white hover:opacity-90',
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
