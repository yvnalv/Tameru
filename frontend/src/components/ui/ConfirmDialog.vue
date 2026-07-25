<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { useConfirmStore } from '@/stores/confirm';
import AppButton from '@/components/ui/AppButton.vue';

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
      <div class="w-full max-w-sm rounded-card border border-border bg-surface p-5 shadow-lift" role="dialog" aria-modal="true">
        <h2 v-if="confirm.pending.title" class="text-base font-semibold">{{ confirm.pending.title }}</h2>
        <p class="text-sm text-text-muted" :class="{ 'mt-1': confirm.pending.title }">{{ confirm.pending.message }}</p>
        <div class="mt-5 flex justify-end gap-2">
          <AppButton variant="secondary" @click="confirm.respond(false)">{{ $t('common.cancel') }}</AppButton>
          <AppButton :variant="confirm.pending.danger ? 'danger' : 'primary'" @click="confirm.respond(true)">
            {{ confirm.pending.confirmLabel ?? $t('common.confirm') }}
          </AppButton>
        </div>
      </div>
    </div>
  </Teleport>
</template>
