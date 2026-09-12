<script setup lang="ts">
/**
 * Brand lockup: the mark plus the "Tameru" wordmark.
 *
 * The wordmark is real HTML text, not SVG <text>. An SVG referenced through <img src> is an
 * isolated document: it cannot see the page's self-hosted Inter @font-face, and `currentColor`
 * there resolves to black rather than the page's text colour — which previously rendered "Ta"
 * invisibly against the near-black sidebar. Inline markup keeps the wordmark on the same font
 * and colour tokens as the rest of the UI.
 */
withDefaults(defineProps<{ size?: 'sm' | 'md' }>(), { size: 'sm' });
</script>

<template>
  <span class="inline-flex items-center gap-2 text-text" role="img" aria-label="Tameru">
    <svg
      :class="size === 'md' ? 'h-8 w-8' : 'h-7 w-7'"
      viewBox="0 0 40 40"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
      focusable="false"
    >
      <rect width="40" height="40" rx="11" fill="var(--accent)" />
      <!-- Accumulating bars / stacked coins: rising, flat fills, dark motif -->
      <rect x="10" y="22" width="6" height="8" rx="2" fill="var(--accent-contrast)" />
      <rect x="17" y="17" width="6" height="13" rx="2" fill="var(--accent-contrast)" />
      <rect x="24" y="11" width="6" height="19" rx="2" fill="var(--accent-contrast)" />
    </svg>
    <span
      class="font-bold tracking-[-0.01em]"
      :class="size === 'md' ? 'text-[22px]' : 'text-[19px]'"
      aria-hidden="true"
    >
      Ta<span class="text-accent">meru</span>
    </span>
  </span>
</template>
