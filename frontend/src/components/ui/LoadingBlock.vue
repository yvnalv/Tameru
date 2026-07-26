<script setup lang="ts">
import Skeleton from '@/components/ui/Skeleton.vue';

// A generic loading placeholder: a hero card + a couple of list cards. Used in view loading states in
// place of a "Loading…" label. Respects prefers-reduced-motion (see main.css).
withDefaults(defineProps<{ rows?: number }>(), { rows: 4 });
</script>

<template>
  <div class="space-y-4" aria-busy="true">
    <div class="rounded-card border border-border bg-surface p-6">
      <Skeleton class="h-3 w-24" />
      <Skeleton class="mt-3 h-8 w-56" rounded="rounded-lg" />
      <Skeleton class="mt-3 h-2.5 w-full" rounded="rounded-full" />
    </div>
    <div class="rounded-card border border-border bg-surface p-5">
      <Skeleton class="h-3 w-32" />
      <div class="mt-4 space-y-3">
        <div v-for="i in rows" :key="i" class="flex items-center justify-between gap-4">
          <Skeleton class="h-9 w-9 shrink-0" rounded="rounded-full" />
          <div class="flex-1 space-y-1.5">
            <Skeleton class="h-3 w-1/3" />
            <Skeleton class="h-2.5 w-1/4" />
          </div>
          <Skeleton class="h-3 w-20" />
        </div>
      </div>
    </div>
  </div>
</template>
