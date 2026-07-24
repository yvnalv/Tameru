<script setup lang="ts">
import { computed } from 'vue';

// Segmented multi-color spend bar (DESIGN_LANGUAGE.md → category spectrum). Flat fills, no gradient.
const props = defineProps<{ segments: { label: string; value: number }[] }>();

const spectrum = ['var(--cat-1)', 'var(--cat-2)', 'var(--cat-3)', 'var(--cat-4)', 'var(--cat-5)', 'var(--cat-6)', 'var(--cat-7)'];

const total = computed(() => props.segments.reduce((sum, s) => sum + Math.max(0, s.value), 0));

const parts = computed(() =>
  props.segments.map((s, i) => ({
    label: s.label,
    pct: total.value > 0 ? (Math.max(0, s.value) / total.value) * 100 : 0,
    color: spectrum[i % spectrum.length],
  })),
);
</script>

<template>
  <div class="flex h-2.5 w-full overflow-hidden rounded-full bg-surface-2">
    <div
      v-for="(part, i) in parts"
      :key="i"
      class="h-full"
      :style="{ width: `${part.pct}%`, backgroundColor: part.color }"
      :title="part.label"
    />
  </div>
</template>
