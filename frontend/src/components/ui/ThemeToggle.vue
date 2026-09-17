<script setup lang="ts">
import { computed } from 'vue';
import { Sun, Moon } from 'lucide-vue-next';
import { useThemeStore } from '@/stores/theme';
import { useI18n } from 'vue-i18n';

interface Props {
  variant?: 'icon' | 'segmented';
  size?: number;
}

withDefaults(defineProps<Props>(), {
  variant: 'segmented',
  size: 15,
});

const themeStore = useThemeStore();
const { t } = useI18n();

const isDark = computed(() => themeStore.isDark);
const lightLabel = computed(() => t('common.switchToLight', 'Switch to light mode'));
const darkLabel = computed(() => t('common.switchToDark', 'Switch to dark mode'));
const currentLabel = computed(() => (isDark.value ? lightLabel.value : darkLabel.value));
</script>

<template>
  <!-- Segmented Dual-Icon Capsule Switch (Modern, tactile, no text labels) -->
  <div
    v-if="variant === 'segmented'"
    class="inline-flex h-9 items-center rounded-full border border-border bg-surface-2 p-0.5 shadow-xs transition-colors"
    role="radiogroup"
    :aria-label="currentLabel"
  >
    <!-- Light Mode Touch Target -->
    <button
      type="button"
      class="group/light relative flex h-7 w-7 items-center justify-center rounded-full transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
      :class="
        !isDark
          ? 'bg-surface text-amber-500 shadow-sm ring-1 ring-border/50 scale-100'
          : 'text-text-muted/70 hover:text-text hover:scale-95 scale-90'
      "
      role="radio"
      :aria-checked="!isDark"
      :title="lightLabel"
      :aria-label="lightLabel"
      @click="themeStore.setTheme('light')"
    >
      <Sun :size="size" :stroke-width="2" class="transition-transform duration-200 group-hover/light:rotate-45" />
    </button>

    <!-- Dark Mode Touch Target -->
    <button
      type="button"
      class="group/dark relative flex h-7 w-7 items-center justify-center rounded-full transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
      :class="
        isDark
          ? 'bg-surface text-indigo-400 shadow-sm ring-1 ring-border/50 scale-100'
          : 'text-text-muted/70 hover:text-text hover:scale-95 scale-90'
      "
      role="radio"
      :aria-checked="isDark"
      :title="darkLabel"
      :aria-label="darkLabel"
      @click="themeStore.setTheme('dark')"
    >
      <Moon :size="size" :stroke-width="2" class="transition-transform duration-200 group-hover/dark:-rotate-12" />
    </button>
  </div>

  <!-- Single Icon-only Toggle Switch -->
  <button
    v-else
    type="button"
    class="relative inline-flex h-9 w-9 items-center justify-center rounded-control border border-border bg-surface text-text-muted shadow-xs transition-all duration-200 hover:border-border-strong hover:bg-surface-2 hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
    :title="currentLabel"
    :aria-label="currentLabel"
    role="switch"
    :aria-checked="isDark"
    @click="themeStore.toggleTheme()"
  >
    <Sun
      v-if="!isDark"
      :size="size + 2"
      :stroke-width="2"
      class="text-amber-500 transition-transform duration-200 hover:rotate-45"
    />
    <Moon
      v-else
      :size="size + 2"
      :stroke-width="2"
      class="text-indigo-400 transition-transform duration-200 hover:-rotate-12"
    />
  </button>
</template>
