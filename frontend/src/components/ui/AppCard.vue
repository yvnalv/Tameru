<script setup lang="ts">
withDefaults(defineProps<{ padded?: boolean }>(), { padded: true });
</script>

<template>
  <section
    class="rounded-card border border-border bg-surface shadow-lift"
    :class="padded ? 'p-5' : ''"
  >
    <slot />
  </section>
</template>
