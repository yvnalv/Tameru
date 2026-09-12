<script setup lang="ts">
import { onMounted, onUnmounted, ref, useId } from 'vue';
import { useI18n } from 'vue-i18n';
import { X } from 'lucide-vue-next';
import { useFocusTrap } from '@/composables/useFocusTrap';
import { useConfirmStore } from '@/stores/confirm';

const props = withDefaults(defineProps<{ title: string; dirty?: boolean }>(), { dirty: false });
const emit = defineEmits<{ close: [] }>();

const { t } = useI18n();
const confirm = useConfirmStore();
const dialog = ref<HTMLElement | null>(null);
const titleId = useId();

useFocusTrap(dialog);

/** Dismissing by Escape or backdrop click must not silently discard a part-filled form. */
async function requestClose(): Promise<void> {
  if (props.dirty) {
    const ok = await confirm.ask({
      title: t('common.discardTitle'),
      message: t('common.discardMessage'),
      confirmLabel: t('common.discard'),
      danger: true,
    });
    if (!ok) return;
  }
  emit('close');
}

function onKey(e: KeyboardEvent): void {
  // Ignore Escape while the discard confirmation is on top of this modal.
  if (e.key === 'Escape' && !confirm.pending) void requestClose();
}

onMounted(() => document.addEventListener('keydown', onKey));
onUnmounted(() => document.removeEventListener('keydown', onKey));
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed inset-0 z-50 flex items-end justify-center bg-black/60 p-0 sm:items-center sm:p-4"
      @click.self="requestClose"
    >
      <div
        ref="dialog"
        class="w-full max-w-lg rounded-t-card border border-border bg-surface shadow-lift sm:rounded-card"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        tabindex="-1"
      >
        <header class="flex items-center justify-between border-b border-border px-5 py-4">
          <h2 :id="titleId" class="text-base font-semibold">{{ title }}</h2>
          <button
            class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text"
            :aria-label="t('common.close')"
            @click="requestClose"
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
