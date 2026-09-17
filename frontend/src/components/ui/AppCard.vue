<script setup lang="ts">
withDefaults(defineProps<{ padded?: boolean }>(), { padded: true });
</script>

<template>
  <section
    class="rounded-card border border-border bg-surface shadow-card"
    :class="padded ? 'p-5 md:p-6' : ''"
  >
    <slot />
  </section>
</template>
