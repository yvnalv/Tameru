<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { navItems } from '@/components/layout/navItems';
import logoLockup from '@/assets/brand/logo-lockup.svg';
</script>

<template>
  <aside class="hidden w-[248px] shrink-0 flex-col border-r border-border bg-sidebar md:flex">
    <div class="flex h-16 items-center px-5 text-text">
      <img :src="logoLockup" alt="Tameru" class="h-7" />
    </div>

    <nav class="flex-1 space-y-1 px-3 py-2">
      <template v-for="item in navItems" :key="item.key">
        <RouterLink
          v-if="item.route"
          :to="{ name: item.route }"
          class="flex items-center gap-3 rounded-control px-3 py-2 text-sm font-medium text-text-muted hover:bg-surface-2 hover:text-text"
          active-class="!bg-accent-soft !text-accent"
        >
          <component :is="item.icon" :size="20" :stroke-width="1.5" />
          {{ $t(`nav.${item.key}`) }}
        </RouterLink>
        <span
          v-else
          class="flex cursor-default items-center gap-3 rounded-control px-3 py-2 text-sm font-medium text-text-muted/60"
          :title="$t('common.comingSoon')"
        >
          <component :is="item.icon" :size="20" :stroke-width="1.5" />
          {{ $t(`nav.${item.key}`) }}
        </span>
      </template>
    </nav>
  </aside>
</template>
