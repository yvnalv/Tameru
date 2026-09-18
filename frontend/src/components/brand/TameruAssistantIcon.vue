<script setup lang="ts">
/**
 * Tameru Financial Copilot Icon
 * Custom brand-derived emblem representing financial planning canopy and pillars.
 * Free of generic 'AI sparkles' clichés.
 */
withDefaults(
  defineProps<{
    size?: number;
  }>(),
  {
    size: 20,
  }
);
</script>

<template>
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 24 24"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    aria-hidden="true"
    focusable="false"
  >
    <!-- Protective Planning Canopy -->
    <rect x="3.5" y="4" width="17" height="3" rx="1.5" fill="currentColor" />
    <!-- Pillars of financial accumulation -->
    <rect x="4.5" y="14" width="3.5" height="6" rx="1.75" fill="currentColor" opacity="0.75" />
    <rect x="10.25" y="7.5" width="3.5" height="12.5" rx="1.75" fill="currentColor" />
    <rect x="16" y="10.5" width="3.5" height="9.5" rx="1.75" fill="currentColor" opacity="0.9" />
  </svg>
</template>
