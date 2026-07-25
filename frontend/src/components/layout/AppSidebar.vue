<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { PanelLeftClose, PanelLeftOpen } from 'lucide-vue-next';
import { navItems } from '@/components/layout/navItems';
import { useUiStore } from '@/stores/ui';
import logoLockup from '@/assets/brand/logo-lockup.svg';
import logoMark from '@/assets/brand/logo-mark.svg';

const ui = useUiStore();
</script>

<template>
  <aside
    class="hidden shrink-0 flex-col border-r border-border bg-sidebar transition-[width] duration-200 md:flex"
    :class="ui.sidebarCollapsed ? 'w-[72px]' : 'w-[248px]'"
  >
    <!-- Header: logo + collapse toggle -->
    <div class="flex h-16 items-center px-3" :class="ui.sidebarCollapsed ? 'justify-center' : 'justify-between px-5'">
      <img v-if="!ui.sidebarCollapsed" :src="logoLockup" alt="Tameru" class="h-7" />
      <img v-else :src="logoMark" alt="Tameru" class="h-8 w-8" />
      <button
        v-if="!ui.sidebarCollapsed"
        class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text"
        :aria-label="$t('common.collapse')"
        @click="ui.toggleSidebar()"
      >
        <PanelLeftClose :size="18" :stroke-width="1.5" />
      </button>
    </div>

    <!-- Expand button (collapsed only) -->
    <button
      v-if="ui.sidebarCollapsed"
      class="mx-auto mb-1 rounded-control p-2 text-text-muted hover:bg-surface-2 hover:text-text"
      :aria-label="$t('common.expand')"
      @click="ui.toggleSidebar()"
    >
      <PanelLeftOpen :size="18" :stroke-width="1.5" />
    </button>

    <nav class="flex-1 space-y-1 px-3 py-2">
      <RouterLink
        v-for="item in navItems"
        :key="item.key"
        :to="{ name: item.route }"
        class="group/tt relative flex items-center rounded-control text-sm font-medium text-text-muted hover:bg-surface-2 hover:text-text"
        :class="ui.sidebarCollapsed ? 'justify-center p-2.5' : 'gap-3 px-3 py-2'"
        active-class="!bg-accent-soft !text-accent"
      >
        <component :is="item.icon" :size="20" :stroke-width="1.5" />
        <span v-if="!ui.sidebarCollapsed" class="flex-1">{{ $t(`nav.${item.key}`) }}</span>
        <span
          v-if="!ui.sidebarCollapsed && item.placeholder"
          class="rounded-full bg-surface-2 px-1.5 py-0.5 text-[10px] font-medium text-text-muted"
        >{{ $t('common.soon') }}</span>

        <!-- Tooltip when collapsed -->
        <span
          v-if="ui.sidebarCollapsed"
          class="pointer-events-none absolute left-full top-1/2 z-50 ml-2 -translate-y-1/2 whitespace-nowrap rounded-md border border-border bg-surface-2 px-2 py-1 text-xs font-medium text-text opacity-0 shadow-lift transition-opacity group-hover/tt:opacity-100"
          role="tooltip"
        >{{ $t(`nav.${item.key}`) }}</span>
      </RouterLink>
    </nav>
  </aside>
</template>
