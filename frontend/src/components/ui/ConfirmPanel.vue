<script setup lang="ts">
import { ref, useId } from 'vue';
import { useFocusTrap } from '@/composables/useFocusTrap';
import AppButton from '@/components/ui/AppButton.vue';

defineProps<{
  message: string;
  title?: string;
  confirmLabel?: string;
  danger?: boolean;
}>();

const emit = defineEmits<{ respond: [ok: boolean] }>();

// Mounted once per confirmation, so the trap moves focus in here and restores it on close.
const panel = ref<HTMLElement | null>(null);
const titleId = useId();
const descId = useId();
useFocusTrap(panel);
</script>

<template>
  <div
    ref="panel"
    class="w-full max-w-sm rounded-card border border-border bg-surface p-5 shadow-lift"
    role="alertdialog"
    aria-modal="true"
    :aria-labelledby="title ? titleId : undefined"
    :aria-describedby="descId"
    tabindex="-1"
  >
    <h2 v-if="title" :id="titleId" class="text-base font-semibold">{{ title }}</h2>
    <p :id="descId" class="text-sm text-text-muted" :class="{ 'mt-1': title }">{{ message }}</p>
    <div class="mt-5 flex justify-end gap-2">
      <AppButton variant="secondary" @click="emit('respond', false)">
        {{ $t('common.cancel') }}
      </AppButton>
      <AppButton :variant="danger ? 'danger' : 'primary'" @click="emit('respond', true)">
        {{ confirmLabel ?? $t('common.confirm') }}
      </AppButton>
    </div>
  </div>
</template>
