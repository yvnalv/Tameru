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
  <span class="inline-flex items-center gap-2.5 text-text" role="img" aria-label="Tameru">
    <svg
      :class="size === 'md' ? 'h-8 w-8' : 'h-7 w-7'"
      viewBox="0 0 40 40"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
      focusable="false"
    >
      <rect width="40" height="40" rx="11" fill="var(--accent)" />
      <!-- Protective Planning Canopy (Top bar of 'T' representing Budget & Financial Security) -->
      <rect x="8" y="9" width="24" height="4.5" rx="2.25" fill="var(--accent-contrast)" />
      <!-- Ascending Savings / Accumulation Pillars -->
      <rect x="9.5" y="23" width="5" height="8" rx="2.5" fill="var(--accent-contrast)" opacity="0.75" />
      <rect x="17.5" y="13.5" width="5" height="17.5" rx="2.5" fill="var(--accent-contrast)" />
      <rect x="25.5" y="17" width="5" height="14" rx="2.5" fill="var(--accent-contrast)" opacity="0.9" />
    </svg>
    <span
      class="font-bold tracking-tight text-text"
      :class="size === 'md' ? 'text-[22px]' : 'text-[19px]'"
      aria-hidden="true"
    >
      Tameru
    </span>
  </span>
</template>
