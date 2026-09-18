<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { PanelLeftClose, PanelLeftOpen } from 'lucide-vue-next';
import { navItems } from '@/components/layout/navItems';
import { useUiStore } from '@/stores/ui';
import logoMark from '@/assets/brand/logo-mark.svg';
import LogoLockup from '@/components/brand/LogoLockup.vue';

const ui = useUiStore();
</script>

<template>
  <aside
    class="fixed inset-y-0 left-0 z-40 hidden h-screen flex-col border-r border-border bg-sidebar transition-[width] duration-200 md:flex"
    :class="ui.sidebarCollapsed ? 'w-[72px]' : 'w-[240px]'"
  >
    <!-- Header: logo (Fixed at top of sidebar) -->
    <div
      class="flex h-16 shrink-0 items-center border-b border-border"
      :class="ui.sidebarCollapsed ? 'justify-center px-3' : 'px-5'"
    >
      <LogoLockup v-if="!ui.sidebarCollapsed" />
      <img v-else :src="logoMark" alt="Tameru" class="h-8 w-8" />
    </div>

    <!-- Scrollable Navigation Menu (Only this section is scrollable) -->
    <nav class="flex-1 overflow-y-auto scroll-slim space-y-1.5 px-3 py-4">
      <RouterLink
        v-for="item in navItems"
        :key="item.key"
        :to="{ name: item.route }"
        class="group/tt relative flex items-center rounded-control text-sm font-medium transition-all duration-150"
        :class="
          ui.sidebarCollapsed
            ? 'h-10 w-10 mx-auto justify-center p-0 text-text-muted hover:bg-surface-2 hover:text-text'
            : 'gap-3 px-3 py-2.5 text-text-muted hover:bg-surface-2 hover:text-text'
        "
        active-class="!bg-accent !text-accent-contrast font-semibold shadow-sm"
      >
        <component :is="item.icon" :size="20" :stroke-width="1.75" />
        <span v-if="!ui.sidebarCollapsed" class="flex-1 truncate">{{ $t(`nav.${item.key}`) }}</span>
        <span
          v-if="!ui.sidebarCollapsed && item.placeholder"
          class="rounded-full bg-surface-2 px-1.5 py-0.5 text-[10px] font-medium text-text-muted"
        >{{ $t('common.soon') }}</span>

        <!-- Tooltip when collapsed -->
        <span
          v-if="ui.sidebarCollapsed"
          class="pointer-events-none absolute left-full top-1/2 z-50 ml-2.5 -translate-y-1/2 whitespace-nowrap rounded-control border border-border bg-surface px-2.5 py-1 text-xs font-semibold text-text opacity-0 shadow-popover transition-opacity group-hover/tt:opacity-100"
          role="tooltip"
        >{{ $t(`nav.${item.key}`) }}</span>
      </RouterLink>
    </nav>

    <!-- Footer: Pinned at bottom of sidebar -->
    <div
      class="shrink-0 border-t border-border p-3 text-xs text-text-muted"
      :class="ui.sidebarCollapsed ? 'flex justify-center' : 'flex items-center justify-between'"
    >
      <span v-if="!ui.sidebarCollapsed" class="text-[11px] font-medium opacity-70">Tameru v0.1</span>
      <button
        type="button"
        class="rounded-control p-1.5 text-text-muted hover:bg-surface-2 hover:text-text transition-colors"
        :title="ui.sidebarCollapsed ? $t('common.expand') : $t('common.collapse')"
        @click="ui.toggleSidebar()"
      >
        <component :is="ui.sidebarCollapsed ? PanelLeftOpen : PanelLeftClose" :size="16" />
      </button>
    </div>
  </aside>
</template>
