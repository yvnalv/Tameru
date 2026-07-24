<script setup lang="ts">
import { computed } from 'vue';

// Circular initials chip for accounts/people/merchants (DESIGN_LANGUAGE.md → avatar chips).
const props = defineProps<{ name: string }>();

const initials = computed(() =>
  props.name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]?.toUpperCase() ?? '')
    .join(''),
);
</script>

<template>
  <span
    class="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-surface-2 text-xs font-medium text-text"
    :title="name"
  >
    {{ initials }}
  </span>
</template>
