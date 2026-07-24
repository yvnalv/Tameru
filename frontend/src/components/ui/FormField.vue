<script setup lang="ts">
defineProps<{ label: string; forId?: string }>();
</script>

<template>
  <div class="space-y-1.5">
    <label :for="forId" class="block text-[13px] font-medium text-text-muted">{{ label }}</label>
    <slot />
  </div>
</template>
