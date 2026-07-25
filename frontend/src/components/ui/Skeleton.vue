<script setup lang="ts">
// A single shimmering placeholder block. Size it with utility classes (h-*, w-*).
withDefaults(defineProps<{ rounded?: string }>(), { rounded: 'rounded' });
</script>

<template>
  <div class="animate-pulse bg-surface-2" :class="rounded" />
</template>
