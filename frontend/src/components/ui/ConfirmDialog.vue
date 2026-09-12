<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useConfirmStore } from '@/stores/confirm';
import ConfirmPanel from '@/components/ui/ConfirmPanel.vue';

const confirm = useConfirmStore();

function onKey(e: KeyboardEvent): void {
  if (!confirm.pending) return;
  if (e.key === 'Escape') confirm.respond(false);
}
onMounted(() => document.addEventListener('keydown', onKey));
onUnmounted(() => document.removeEventListener('keydown', onKey));
</script>

<template>
  <Teleport to="body">
    <div
      v-if="confirm.pending"
      class="fixed inset-0 z-[70] flex items-center justify-center bg-black/60 p-4"
      @click.self="confirm.respond(false)"
    >
      <!-- Keyed per confirmation so the panel remounts and focus moves into it every time. -->
      <ConfirmPanel
        :key="confirm.pending.message"
        :message="confirm.pending.message"
        :title="confirm.pending.title"
        :confirm-label="confirm.pending.confirmLabel"
        :danger="confirm.pending.danger"
        @respond="confirm.respond($event)"
      />
    </div>
  </Teleport>
</template>
