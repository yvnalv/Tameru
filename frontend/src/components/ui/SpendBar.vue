<script setup lang="ts">
import { computed } from 'vue';
import { spectrumColor } from '@/lib/spectrum';

// Segmented multi-color spend bar (DESIGN_LANGUAGE.md → category spectrum). Flat fills, no gradient.
const props = withDefaults(
  defineProps<{
    segments: { label: string; value: number }[];
    /** Announced to screen readers; the bar is a graphic, so it needs a text equivalent. */
    label?: string;
  }>(),
  { label: '' },
);

const total = computed(() => props.segments.reduce((sum, s) => sum + Math.max(0, s.value), 0));

const parts = computed(() =>
  props.segments.map((s, i) => ({
    label: s.label,
    pct: total.value > 0 ? (Math.max(0, s.value) / total.value) * 100 : 0,
    color: spectrumColor(i),
  })),
);

/** "Food 36%, Entertainment 20%, …" — the same information the colours carry visually. */
const description = computed(() => {
  const parted = parts.value.map((p) => `${p.label} ${Math.round(p.pct)}%`).join(', ');
  return props.label ? `${props.label}: ${parted}` : parted;
});
</script>

<template>
  <div
    class="flex h-2.5 w-full overflow-hidden rounded-full bg-surface-2"
    role="img"
    :aria-label="description"
  >
    <div
      v-for="(part, i) in parts"
      :key="i"
      class="h-full"
      :style="{ width: `${part.pct}%`, backgroundColor: part.color }"
      :title="`${part.label} — ${Math.round(part.pct)}%`"
    />
  </div>
</template>
