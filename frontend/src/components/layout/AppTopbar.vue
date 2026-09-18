<script setup lang="ts">
import { useRouter } from 'vue-router';
import {
  LogOut,
  Languages,
  PanelLeftClose,
  PanelLeftOpen,
  Eye,
  EyeOff,
  Sparkles,
  Search,
} from 'lucide-vue-next';
import { useAuthStore } from '@/stores/auth';
import { useUiStore } from '@/stores/ui';
import { useAssistantStore } from '@/stores/assistant';
import IconButton from '@/components/ui/IconButton.vue';
import AvatarChip from '@/components/ui/AvatarChip.vue';
import ThemeToggle from '@/components/ui/ThemeToggle.vue';
import TameruAssistantIcon from '@/components/brand/TameruAssistantIcon.vue';

const router = useRouter();
const auth = useAuthStore();
const ui = useUiStore();
const assistant = useAssistantStore();

async function signOut(): Promise<void> {
  await auth.logout();
  router.push({ name: 'login' });
}
</script>

<template>
  <header
    class="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-border bg-surface/90 px-4 backdrop-blur shadow-xs md:px-8"
  >
    <!-- Left: Sidebar Toggle, Section Title, Quick Add -->
    <div class="flex items-center gap-2.5">
      <IconButton
        class="hidden md:inline-flex"
        variant="outline"
        placement="bottom"
        :icon="ui.sidebarCollapsed ? PanelLeftOpen : PanelLeftClose"
        :label="ui.sidebarCollapsed ? $t('common.expand') : $t('common.collapse')"
        :size="16"
        @click="ui.toggleSidebar()"
      />

      <h1 class="text-base font-bold text-text tracking-tight">
        {{ $t(`nav.${(router.currentRoute.value.name as string) || 'dashboard'}`) }}
      </h1>

      <!-- Omnipresent Command Palette Search Bar Trigger -->
      <button
        type="button"
        class="relative ml-2 hidden sm:flex h-9 w-48 md:w-60 items-center rounded-control border border-border bg-surface-2/80 pl-9 pr-2.5 text-xs text-text-muted transition-all hover:border-accent/40 hover:bg-surface focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        :title="$t('commandPalette.searchAnything') + ' (⌘K / Ctrl+K)'"
        @click="ui.openCommandPalette()"
      >
        <Search :size="15" class="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted" />
        <span class="truncate text-left text-text-muted">{{ $t('commandPalette.searchAnything') }}...</span>
        <kbd class="ml-auto inline-flex items-center rounded border border-border bg-surface px-1.5 py-0.5 text-[10px] font-semibold text-text-muted">
          ⌘K
        </kbd>
      </button>

      <!-- Mobile Search Icon Button -->
      <IconButton
        class="sm:hidden"
        variant="outline"
        :icon="Search"
        :label="$t('commandPalette.searchAnything')"
        :size="16"
        @click="ui.openCommandPalette()"
      />
    </div>

    <!-- Right: Utility Controls (Design System Consistent) -->
    <div class="flex items-center gap-1.5">
      <!-- Privacy Toggle (Hide/Show Amounts) with bottom tooltip -->
      <IconButton
        variant="outline"
        placement="bottom"
        :active="ui.amountsHidden"
        :icon="ui.amountsHidden ? EyeOff : Eye"
        :label="ui.amountsHidden ? $t('common.showAmounts') : $t('common.hideAmounts')"
        :size="16"
        @click="ui.toggleAmounts()"
      />

      <!-- Language Selector (Icon-Only with tooltip) -->
      <IconButton
        variant="outline"
        placement="bottom"
        :icon="Languages"
        :label="`${$t('common.language')} (${ui.locale.toUpperCase()})`"
        :size="16"
        @click="ui.toggleLocale()"
      />

      <!-- Modern Light/Dark Mode Switcher (Icon-Only Capsule) -->
      <ThemeToggle variant="segmented" :size="14" class="mx-0.5" />

      <!-- AI Assistant Button -->
      <button
        type="button"
        class="inline-flex h-9 items-center gap-1.5 rounded-control border border-indigo-500/30 bg-indigo-500/10 px-3 text-xs font-semibold text-indigo-400 shadow-xs transition-all hover:bg-indigo-500 hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500"
        :title="$t('assistant.title') + ' (Ctrl+J)'"
        @click="assistant.openAssistant()"
      >
        <TameruAssistantIcon :size="14" />
        <span class="hidden lg:inline">{{ $t('assistant.title') }}</span>
      </button>

      <!-- Design System Link -->
      <button
        type="button"
        class="hidden sm:inline-flex h-9 items-center gap-1.5 rounded-control border border-accent/30 bg-accent-soft px-3 text-xs font-semibold text-accent shadow-xs transition-all hover:bg-accent hover:text-accent-contrast focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        title="Open Design System Showcase"
        @click="router.push({ name: 'design-system' })"
      >
        <Sparkles :size="14" />
        <span class="hidden lg:inline">Design System</span>
      </button>

      <!-- Sign Out Button with bottom tooltip -->
      <IconButton
        variant="outline"
        placement="bottom"
        danger
        :icon="LogOut"
        :label="$t('common.signOut')"
        :size="16"
        @click="signOut"
      />

      <!-- Logged-in owner Profile Pill (Clickable -> Settings) -->
      <button
        v-if="auth.user"
        type="button"
        class="ml-1 hidden items-center gap-2 border-l border-border pl-3 sm:flex rounded-control p-1 hover:bg-surface-2 transition-colors text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent"
        :title="$t('nav.settings')"
        @click="router.push({ name: 'settings' })"
      >
        <AvatarChip :name="auth.user.displayName || auth.user.email" />
        <div class="hidden leading-tight lg:block">
          <p class="max-w-[8.5rem] truncate text-xs font-semibold text-text">
            {{ auth.user.displayName || auth.user.email }}
          </p>
          <p class="max-w-[8.5rem] truncate text-[11px] text-text-muted font-mono">
            {{ auth.user.email }}
          </p>
        </div>
      </button>
    </div>
  </header>
</template>
