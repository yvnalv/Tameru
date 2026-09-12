<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { RouterLink } from 'vue-router';
import { X } from 'lucide-vue-next';
import { mobileMoreItems } from '@/components/layout/navItems';
import { useFocusTrap } from '@/composables/useFocusTrap';

const emit = defineEmits<{ close: [] }>();

// Mounted only while open, so the trap's lifecycle matches the sheet's.
const sheet = ref<HTMLElement | null>(null);
useFocusTrap(sheet);

function onKey(e: KeyboardEvent): void {
  if (e.key === 'Escape') emit('close');
}
onMounted(() => document.addEventListener('keydown', onKey));
onUnmounted(() => document.removeEventListener('keydown', onKey));
</script>

<template>
  <div class="fixed inset-0 z-40 flex items-end bg-black/60 md:hidden" @click.self="emit('close')">
    <div
      ref="sheet"
      class="w-full rounded-t-card border-t border-border bg-surface pb-[env(safe-area-inset-bottom)]"
      role="dialog"
      aria-modal="true"
      :aria-label="$t('nav.more')"
      tabindex="-1"
    >
      <header class="flex items-center justify-between border-b border-border px-5 py-4">
        <h2 class="text-base font-semibold">{{ $t('nav.more') }}</h2>
        <button
          class="rounded-control p-1 text-text-muted hover:bg-surface-2 hover:text-text"
          :aria-label="$t('common.close')"
          @click="emit('close')"
        >
          <X :size="18" />
        </button>
      </header>
      <ul class="p-2">
        <li v-for="item in mobileMoreItems" :key="item.key">
          <RouterLink
            :to="{ name: item.route }"
            class="flex items-center gap-3 rounded-control px-3 py-3 text-sm font-medium text-text-muted"
            active-class="!bg-accent-soft !text-accent"
            @click="emit('close')"
          >
            <component :is="item.icon" :size="20" :stroke-width="1.5" />
            {{ $t(`nav.${item.key}`) }}
          </RouterLink>
        </li>
      </ul>
    </div>
  </div>
</template>
