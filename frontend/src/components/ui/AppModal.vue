<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { X } from 'lucide-vue-next';

defineProps<{ title: string }>();
const emit = defineEmits<{ close: [] }>();

function onKey(e: KeyboardEvent): void {
  if (e.key === 'Escape') emit('close');
}

onMounted(() => document.addEventListener('keydown', onKey));
onUnmounted(() => document.removeEventListener('keydown', onKey));
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-end justify-center bg-black/60 p-0 sm:items-center sm:p-4"
      @click.self="emit('close')"
    >
      <div
        class="w-full max-w-lg rounded-t-card border border-border bg-surface shadow-lift sm:rounded-card"
        role="dialog"
        aria-modal="true"
      >
        <header class="flex items-center justify-between border-b border-border px-5 py-4">
          <h2 class="text-base font-semibold">{{ title }}</h2>
          <button
            class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text"
            :aria-label="'Close'"
            @click="emit('close')"
          >
            <X :size="18" />
          </button>
        </header>

        <div class="scroll-slim max-h-[70vh] overflow-y-auto px-5 py-4">
          <slot />
        </div>

        <footer v-if="$slots.footer" class="flex justify-end gap-2 border-t border-border px-5 py-4">
          <slot name="footer" />
        </footer>
      </div>
    </div>
  </Teleport>
</template>
