<script setup lang="ts">
import { CircleCheck, CircleAlert, Info, X } from 'lucide-vue-next';
import { useToastStore } from '@/stores/toast';

const toast = useToastStore();

const icon = { success: CircleCheck, error: CircleAlert, info: Info };
const accent = {
  success: 'text-positive',
  error: 'text-negative',
  info: 'text-info',
};
</script>

<template>
  <Teleport to="body">
    <div class="pointer-events-none fixed inset-x-0 bottom-0 z-[60] flex flex-col items-center gap-2 p-4 sm:items-end">
      <TransitionGroup
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="translate-y-2 opacity-0"
        leave-active-class="transition duration-150 ease-in"
        leave-to-class="opacity-0"
      >
        <div
          v-for="t in toast.items"
          :key="t.id"
          class="pointer-events-auto flex w-full max-w-sm items-start gap-2.5 rounded-card border border-border bg-surface px-4 py-3 shadow-lift"
        >
          <component :is="icon[t.kind]" :size="18" class="mt-0.5 shrink-0" :class="accent[t.kind]" />
          <p class="flex-1 text-[13px] leading-snug">{{ t.message }}</p>
          <button class="shrink-0 text-text-muted hover:text-text" :aria-label="'Close'" @click="toast.remove(t.id)">
            <X :size="15" />
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>
