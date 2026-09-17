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
      class="fixed inset-0 z-50 flex items-end justify-center bg-black/60 backdrop-blur-sm p-0 sm:items-center sm:p-4 transition-all"
      @click.self="requestClose"
    >
      <div
        ref="dialog"
        class="flex max-h-[92dvh] sm:max-h-[88vh] w-full max-w-xl flex-col rounded-t-2xl border border-border bg-surface shadow-popover sm:rounded-2xl overflow-hidden"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        tabindex="-1"
      >
        <!-- Mobile Drag Handle Indicator -->
        <div class="sm:hidden pt-2.5 pb-1 flex justify-center shrink-0">
          <div class="h-1 w-10 rounded-full bg-border"></div>
        </div>

        <header class="flex shrink-0 items-center justify-between border-b border-border px-5 py-3 sm:py-3.5">
          <h2 :id="titleId" class="text-base font-bold text-text">{{ title }}</h2>
          <button
            class="rounded-lg p-1.5 text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
            :aria-label="t('common.close')"
            @click="requestClose"
          >
            <X :size="18" />
          </button>
        </header>

        <div class="scroll-slim flex-1 min-h-0 overflow-y-auto px-5 py-4">
          <slot />
        </div>

        <footer
          v-if="$slots.footer"
          class="shrink-0 border-t border-border bg-surface px-5 py-3 sm:py-4 pb-[calc(0.75rem+env(safe-area-inset-bottom,0px))]"
        >
          <slot name="footer" />
        </footer>
      </div>
    </div>
  </Teleport>
</template>
